using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;
using TcgEngine.FX;

namespace TcgEngine.Client
{
    /// <summary>
    /// Represents the visual aspect of a card on the board.
    /// Will take the data from Card.cs and display it
    /// </summary>

    public class BoardCard : MonoBehaviour
    {
        public SpriteRenderer card_sprite;
        public SpriteRenderer card_glow;
        public SpriteRenderer card_shadow;

        public Image armor_icon;
        public Text armor;

        public CanvasGroup status_group;
        public Text status_text;

        public AbilityButton[] buttons;

        public Color glow_ally;
        public Color glow_enemy;

        public UnityAction onKill;

        private CardUI card_ui;
        private BoardCardFX card_fx;
        private Canvas canvas;
        private string card_id = "";
        private string card_uid = "";

        private bool destroyed = false;
        private bool focus = false;
        private float timer = 0f;
        private float status_alpha_target = 0f;

        private bool back_to_hand;
        private Vector3 back_to_hand_target;

        private static List<BoardCard> card_list = new List<BoardCard>();

        void Awake()
        {
            card_list.Add(this);
            card_ui = GetComponent<CardUI>();
            card_fx = GetComponent<BoardCardFX>();
            canvas = GetComponentInChildren<Canvas>();
            card_glow.color = new Color(card_glow.color.r, card_glow.color.g, card_glow.color.b, 0f);
            canvas.gameObject.SetActive(false);
            status_alpha_target = 0f;

            if (status_group != null)
                status_group.alpha = 0f;
        }

        void OnDestroy()
        {
            card_list.Remove(this);
        }

        private void Start()
        {
            //Random slight rotation
            Vector3 board_rot = GameBoard.Get().GetAngles();
            transform.rotation = Quaternion.Euler(board_rot.x, board_rot.y, board_rot.z + Random.Range(-1f, 1f));
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            timer += Time.deltaTime;
            if (timer > 0.15f && !destroyed && !canvas.gameObject.activeSelf)
                canvas.gameObject.SetActive(true);

            PlayerControls controls = PlayerControls.Get();
            Game data = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Card card = data.GetCard(card_uid);
            card_ui.SetCardBoard(card);

            bool selected = controls.GetSelected() == this;
            Vector3 targ_pos = GetTargetPos();
            float speed = 12f;

            transform.position = Vector3.MoveTowards(transform.position, targ_pos, speed * Time.deltaTime);

            float target_alpha = IsFocus() || selected ? 1f : 0f;
            if (destroyed || timer < 1f)
                target_alpha = 0f;

            float calpha = Mathf.MoveTowards(card_glow.color.a, target_alpha, 4f * Time.deltaTime);
            Color ccolor = player.player_id == card.player_id ? glow_ally : glow_enemy;
            card_glow.color = new Color(ccolor.r, ccolor.g, ccolor.b, calpha);
            card_shadow.enabled = !destroyed && timer > 0.4f;
            card_sprite.color = card.HasStatus(StatusType.Stealth) ? Color.gray : Color.white;
            card_ui.hp.color = card.damage > 0 ? Color.yellow : Color.white;

            //armor
            int armor_val = card.GetStatusValue(StatusType.Armor);
            armor.text = armor_val.ToString();
            armor.enabled = armor_val > 0;
            armor_icon.enabled = armor_val > 0;

            //Reset after transform
            Sprite sprite = card.CardData.GetBoardArt(card.variant);
            if (sprite != card_sprite.sprite)
                card_sprite.sprite = sprite;

            //Ability buttons
            foreach (AbilityButton button in buttons)
                button.Hide();

            if (selected && card.player_id == player.player_id)
            {
                int index = 0;
                CardData icard = CardData.Get(card.card_id);
                foreach (AbilityData iability in icard.abilities)
                {
                    if (iability && data.CanCastAbility(card, iability))
                    {
                        if (iability.target != AbilityTarget.Self || iability.AreTargetConditionsMet(data, card, card))
                        {
                            if (index < buttons.Length)
                            {
                                AbilityButton button = buttons[index];
                                button.SetAbility(card, iability);
                            }
                            index++;
                        }
                    }
                }
            }

            //Status bar
            if (status_group != null)
                status_group.alpha = Mathf.MoveTowards(status_group.alpha, status_alpha_target, 5f * Time.deltaTime);
        }

        private Vector3 GetTargetPos()
        {
            PlayerControls controls = PlayerControls.Get();
            Game data = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Card card = data.GetCard(card_uid);

            if (destroyed && back_to_hand && timer > 0.5f)
                return back_to_hand_target;

            BoardSlot slot = BoardSlot.Get(card.slot);
            if (slot != null)
            {
                Vector3 targ_pos = slot.transform.position;
                return targ_pos;
            }

            return transform.position;
        }

        public void SetCard(Card card)
        {
            this.card_id = card.card_id;
            this.card_uid = card.uid;

            transform.position = GetTargetPos();

            CardData icard = CardData.Get(card.card_id);
            if (icard)
            {
                card_ui.SetCardBoard(card);
                card_sprite.sprite = icard.GetBoardArt(card.variant);
                armor.enabled = false;
                armor_icon.enabled = false;
                status_alpha_target = 0f;
            }
        }

        public void SetOrder(int order)
        {
            card_sprite.sortingOrder = order;
            canvas.sortingOrder = order + 1;
        }

        public void Kill()
        {
            if (!destroyed)
            {
                destroyed = true;
                timer = 0f;
                status_alpha_target = 0f;
                card_glow.enabled = false;
                card_shadow.enabled = false;
                SetOrder(card_sprite.sortingOrder - 2);
                Destroy(gameObject, 1.3f);

                TimeTool.WaitFor(0.8f, () =>
                {
                    canvas.gameObject.SetActive(false);
                });

                Game data = GameClient.Get().GetGameData();
                Card card = data.GetCard(card_uid);
                Player player = data.GetPlayer(card.player_id);
                GameBoard board = GameBoard.Get();
                if (player.HasCard(player.cards_hand, card) || player.HasCard(player.cards_deck, card))
                {
                    back_to_hand = true;
                    back_to_hand_target = player.player_id == GameClient.Get().GetPlayerID() ? -board.transform.up : board.transform.up;
                    back_to_hand_target = back_to_hand_target * 10f;
                }


                if (onKill != null)
                    onKill.Invoke();
            }
        }

        private void ShowStatusBar()
        {
            Game data = GameClient.Get().GetGameData();
            Card card = data.GetCard(card_uid);
            if (card != null && status_text != null && !destroyed)
            {
                status_text.text = "";

                foreach (CardStatus astatus in card.GetAllStatus())
                {
                    StatusData istats = StatusData.Get(astatus.type);
                    if (istats != null && !string.IsNullOrEmpty(istats.title))
                    {
                        int ival = Mathf.Max(astatus.value, Mathf.CeilToInt(astatus.duration / 2f));
                        string sval = ival > 1 ? " " + ival : "";
                        status_text.text += istats.GetTitle() + sval + ", ";
                    }
                }

                if (status_text.text.Length > 2)
                    status_text.text = status_text.text.Substring(0, status_text.text.Length - 2);
            }

            bool show_status = status_text != null && status_text.text.Length > 0;
            status_alpha_target = show_status ? 1f : 0f;
        }

        public bool IsDead()
        {
            return destroyed;
        }

        public bool IsFocus()
        {
            return focus;
        }

        public void OnMouseEnter()
        {
            if (GameUI.IsUIOpened())
                return;

            if (GameTool.IsMobile())
                return;

            focus = true;
            ShowStatusBar();
        }

        public void OnMouseExit()
        {
            focus = false;
            status_alpha_target = 0f;
        }

        public void OnMouseDown()
        {
            if (GameUI.IsOverUI())
                return;

            PlayerControls.Get().SelectCard(this);

            if (GameTool.IsMobile())
            {
                focus = true;
                ShowStatusBar();
            }
        }

        public void OnMouseUp()
        {

        }

        public void OnMouseOver()
        {
            if (Input.GetMouseButtonDown(1))
            {
                Game gdata = GameClient.Get().GetGameData();
                int player_id = GameClient.Get().GetPlayerID();
                if (gdata.state == GameState.Play && player_id == gdata.current_player)
                {
                    PlayerControls.Get().SelectCardRight(this);
                }
            }
        }

        public string GetCardUID()
        {
            return card_uid;
        }

        public Card GetCard()
        {
            Game data = GameClient.Get().GetGameData();
            Card card = data.GetCard(card_uid);
            return card;
        }

        public CardData GetCardData()
        {
            Card card = GetCard();
            if (card != null)
                return CardData.Get(card.card_id);
            return null;
        }

        public Slot GetSlot()
        {
            return GetCard().slot;
        }

        public BoardCardFX GetCardFX()
        {
            return card_fx;
        }

        public CardData CardData { get { return GetCardData(); } }

        public static int GetNbCardsBoardPlayer(int player_id)
        {
            int nb = 0;
            foreach (BoardCard acard in card_list)
            {
                if (acard != null && acard.GetCard().player_id == player_id)
                    nb++;
            }
            return nb;
        }

        public static BoardCard GetNearestPlayer(Vector3 pos, int skip_player_id, BoardCard skip, float range = 2f)
        {
            BoardCard nearest = null;
            float min_dist = range;
            foreach (BoardCard card in card_list)
            {
                float dist = (card.transform.position - pos).magnitude;
                if (dist < min_dist && card != skip && skip_player_id != card.GetCard().player_id)
                {
                    min_dist = dist;
                    nearest = card;
                }
            }
            return nearest;
        }

        public static BoardCard GetNearest(Vector3 pos, BoardCard skip, float range = 2f)
        {
            BoardCard nearest = null;
            float min_dist = range;
            foreach (BoardCard card in card_list)
            {
                float dist = (card.transform.position - pos).magnitude;
                if (dist < min_dist && card != skip)
                {
                    min_dist = dist;
                    nearest = card;
                }
            }
            return nearest;
        }

        public static BoardCard GetFocus()
        {
            foreach (BoardCard card in card_list)
            {
                if (card.IsFocus())
                    return card;
            }
            return null;
        }

        public static void UnfocusAll()
        {
            foreach (BoardCard card in card_list)
            {
                card.focus = false;
                card.status_alpha_target = 0f;
            }
        }

        public static BoardCard Get(string uid)
        {
            foreach (BoardCard card in card_list)
            {
                if (card.card_uid == uid)
                    return card;
            }
            return null;
        }

        public static List<BoardCard> GetAll()
        {
            return card_list;
        }
    }
}