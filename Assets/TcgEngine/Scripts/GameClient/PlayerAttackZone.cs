using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Visual zone that can be attacked by opponent's card to damage the player HP
    /// </summary>

    public class PlayerAttackZone : MonoBehaviour
    {
        public bool opponent;

        private SpriteRenderer render;
        private float current_alpha = 0f;
        private float max_alpha = 1f;

        private static PlayerAttackZone instance_self;
        private static PlayerAttackZone instance_other;

        private static List<PlayerAttackZone> zone_list = new List<PlayerAttackZone>();

        void Awake()
        {
            zone_list.Add(this);
            if (opponent)
                instance_other = this;
            else
                instance_self = this;
            render = GetComponent<SpriteRenderer>();
            max_alpha = render.color.a;
            render.color = new Color(render.color.r, render.color.g, render.color.b, 0f);
        }

        private void OnDestroy()
        {
            zone_list.Remove(this);
        }

        private void Start()
        {
            GameClient.Get().onAbilityTargetPlayer += OnAbilityEffect;

        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            if (!opponent)
                return;

            //int player_id = opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
            BoardCard bcard_selected = PlayerControls.Get().GetSelected();
            HandCard drag_card = HandCard.GetDrag();
            bool your_turn = GameClient.Get().IsYourTurn();

            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Player oplayer = GameClient.Get().GetOpponentPlayer();

            float target_alpha = 0f;
            Card select_card = bcard_selected?.GetCard();
            if (select_card != null)
            {
                bool can_do_attack = gdata.IsPlayerActionTurn(player) && select_card.CanAttack();
                bool can_be_attacked = gdata.CanAttackTarget(select_card, oplayer);

                if (can_do_attack && can_be_attacked)
                {
                    target_alpha = 1f;
                }
            }

            if (your_turn && drag_card != null && drag_card.CardData.IsRequireTarget() && gdata.IsPlayTargetValid(drag_card.GetCard(), GetPlayer()))
            {
                target_alpha = 1f; //Highlight when dragin a spell with target
            }

            if (gdata.selector == SelectorType.SelectTarget && player.player_id == gdata.selector_player)
            {
                Card caster = gdata.GetCard(gdata.selector_caster_uid);
                AbilityData ability = AbilityData.Get(gdata.selector_ability_id);
                if (ability != null && ability.AreTargetConditionsMet(gdata, caster, GetPlayer()))
                    target_alpha = 1f; //Highlight when selecting a target and empty slots are valid
            }

            current_alpha = Mathf.MoveTowards(current_alpha, target_alpha * max_alpha, 2f * Time.deltaTime);
            render.color = new Color(render.color.r, render.color.g, render.color.b, current_alpha);
        }

        private void OnAbilityEffect(AbilityData iability, Card caster, Player target)
        {
            if (iability != null && caster != null && target != null)
            {
                int player_id = opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
                if (target.player_id == player_id)
                {
                    if (iability.target_fx != null)
                        Instantiate(iability.target_fx, transform.position, Quaternion.identity);

                    AudioTool.Get().PlaySFX("fx", iability.target_audio);
                }
            }
        }

        public void OnMouseDown()
        {
            if (GameUI.IsUIOpened())
                return;

            Game gdata = GameClient.Get().GetGameData();
            int player_id = GameClient.Get().GetPlayerID();
            if (gdata.selector == SelectorType.SelectTarget && player_id == gdata.selector_player)
            {
                GameClient.Get().SelectPlayer(GetPlayer());
            }
        }

        public int GetPlayerID()
        {
            return opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
        }

        public Player GetPlayer()
        {
            return opponent ? GameClient.Get().GetOpponentPlayer() : GameClient.Get().GetPlayer();
        }

        public Slot GetSlot()
        {
            return new Slot(GetPlayerID());
        }

        public static PlayerAttackZone GetNearest(Vector3 pos, float range = 999f)
        {
            PlayerAttackZone nearest = null;
            float min_dist = range;
            foreach (PlayerAttackZone zone in zone_list)
            {
                float dist = (zone.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = zone;
                }
            }
            return nearest;
        }

        public static PlayerAttackZone Get(bool opponent)
        {
            if(opponent)
                return instance_other;
            return instance_self;
        }
    }
}