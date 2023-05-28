using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// Main UI script for all the game scene UI
    /// </summary>

    public class GameUI : MonoBehaviour
    {
        public Canvas game_canvas;
        public Canvas panel_canvas;
        public Canvas top_canvas;
        public UIPanel menu_panel;
        public Text quit_btn;

        [Header("Turn Area")]
        public Text turn_count;
        public Text turn_timer;
        public Button end_turn_button;

        private float selector_timer = 0f;

        private static GameUI _instance;

        void Awake()
        {
            _instance = this;
            game_canvas.worldCamera = Camera.main;
            panel_canvas.worldCamera = Camera.main;
            top_canvas.worldCamera = Camera.main;
        }

        private void Start()
        {
            GameClient.Get().onGameStart += OnGameStart;
            GameClient.Get().onNewTurn += OnNextTurn;
            LoadPanel.Get().Show(true);
            BlackPanel.Get().Show(true);
            BlackPanel.Get().Hide();

            if (quit_btn != null)
                quit_btn.text = GameClient.game_settings.IsOnlinePlayer() ? "Resign" : "Quit";
        }

        void Update()
        {
            Game data = GameClient.Get().GetGameData();
			bool is_connecting = data == null || data.state == GameState.Connecting;
            bool connection_lost = !is_connecting && !GameClient.Get().IsReady();
            ConnectionPanel.Get().SetVisible(connection_lost);

            //Menu
            if (Input.GetKeyDown(KeyCode.Escape))
                menu_panel.Toggle();

            if (!GameClient.Get().IsReady())
                return;

            bool yourturn = GameClient.Get().IsYourTurn();
            int player_id = GameClient.Get().GetPlayerID();

            LoadPanel.Get().SetVisible(is_connecting && !data.AreAllPlayersConnected());
            end_turn_button.interactable = GameClient.Get().IsYourTurn();

            //Timer
            turn_count.text = "Turn " + data.turn_count.ToString();
            turn_timer.enabled = data.turn_timer > 0f;
            turn_timer.text = Mathf.RoundToInt(data.turn_timer).ToString();
            turn_timer.enabled = data.turn_timer < 999f;

            //Simulate timer
            if (data.state == GameState.Play && data.turn_timer > 0f)
                data.turn_timer -= Time.deltaTime;

            //Card target
            bool show_msg = data.selector == SelectorType.SelectTarget && data.selector_player == player_id;
            SelectTargetUI.Get().SetVisible(show_msg);
            if (show_msg)
            {
                AbilityData iability = AbilityData.Get(data.selector_ability_id);
                SelectTargetUI.Get().ShowMsg(iability.title, iability.desc);
            }

            if (!CardSelector.Get().IsVisible())
                selector_timer += Time.deltaTime;

            //Card Selector
            bool show_selector = data.selector == SelectorType.SelectorCard && data.selector_player == player_id && selector_timer > 2f;
            if (show_selector && !CardSelector.Get().IsVisible())
            {
                AbilityData iability = AbilityData.Get(data.selector_ability_id);
                Card caster = data.GetCard(data.selector_caster_uid);
                List<Card> card_list = iability.GetValidCardTargets(data, caster);
                selector_timer = 0f;

                if (iability != null)
                    CardSelector.Get().Show(card_list, iability);
            }

            //Choice selector
            bool show_choice_selector = data.selector == SelectorType.SelectorChoice && data.selector_player == player_id;
            if (show_choice_selector && !ChoiceSelector.Get().IsVisible() && selector_timer > 2f)
            {
                AbilityData iability = AbilityData.Get(data.selector_ability_id);
                ChoiceSelector.Get().Show(iability);
                selector_timer = 0f;
            }

            //Hide
            if (!yourturn && CardSelector.Get().IsAbility())
                CardSelector.Get().Hide();
            if (!yourturn && ChoiceSelector.Get().IsVisible())
                ChoiceSelector.Get().Hide();

        }

        private void OnGameStart()
        {
            Game data = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();
            Player player = data.GetPlayer(player_id);
        }

        private void OnNextTurn(int player_id)
        {
            CardSelector.Get().Hide();
            SelectTargetUI.Get().Hide();
        }

        public void OnClickDeck()
        {
            //GameClient.Get().DrawCard();
        }

        public void OnClickNextTurn()
        {
            GameClient.Get().EndTurn();
        }

        public void OnClickRestart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnClickMenu()
        {
            menu_panel.Show();
        }

        public void OnClickBack()
        {
            menu_panel.Hide();
        }

        public void OnClickQuit()
        {
            bool online = GameClient.game_settings.IsOnlinePlayer();
            bool ended = GameClient.Get().HasEnded();
            if (online && !ended)
                GameClient.Get().Resign();
            else
                StartCoroutine(QuitRoutine("Menu"));
            menu_panel.Hide();
        }

        private IEnumerator QuitRoutine(string scene)
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            AudioTool.Get().FadeOutSFX("ambience");
            AudioTool.Get().FadeOutSFX("ending_sfx");

            yield return new WaitForSeconds(1f);

            GameClient.Get().Disconnect();
            SceneNav.GoTo(scene);
        }

        public void OnClickSwapObserve()
        {
            int other = GameClient.Get().GetPlayerID() == 0 ? 1 : 0;
            GameClient.Get().SetObserverMode(other);
        }

        public static bool IsUIOpened()
        {
            return CardSelector.Get().IsVisible() || EndGamePanel.Get().IsVisible();
        }

        public static bool IsOverUI()
        {
            //return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        public static bool IsOverUILayer(int layer)
        {
            //return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            int count = 0;
            foreach (RaycastResult result in results)
            {
                if (result.sortingLayer == layer)
                    count++;
            }
            return count > 0;
        }

        public static Vector2 ScreenToRectPos(Canvas canvas, RectTransform rect, Vector2 screen_pos)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                Vector2 anchor_pos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen_pos, canvas.worldCamera, out anchor_pos);
                return anchor_pos;
            }
            else
            {
                Vector2 anchor_pos = screen_pos - new Vector2(rect.position.x, rect.position.y);
                anchor_pos = new Vector2(anchor_pos.x / rect.lossyScale.x, anchor_pos.y / rect.lossyScale.y);
                return anchor_pos;
            }
        }

        public static Vector3 MouseToWorld(Vector2 mouse_pos)
        {
            Camera cam = GameCamera.Get() != null ? GameCamera.GetCamera() : Camera.main;
            Vector3 wpos = cam.ScreenToWorldPoint(mouse_pos);
            wpos.z = 0f;
            return wpos;
        }

        public static string FormatNumber(int value)
        {
            return string.Format("{0:#,0}", value);
        }

        public static GameUI Get()
        {
            return _instance;
        }
    }
}
