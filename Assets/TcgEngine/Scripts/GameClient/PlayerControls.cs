using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Script that contain main controls for clicking on cards, attacking, activating abilities
    /// Holds the currently selected card and will send action to GameClient on click release
    /// </summary>

    public class PlayerControls : MonoBehaviour
    {
        private BoardCard selected_card = null;

        private static PlayerControls instance;

        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            if (Input.GetMouseButtonDown(1))
                UnselectAll();

            if (selected_card != null)
            {
                if (Input.GetMouseButtonUp(0))
                    ReleaseClick();
            }
        }

        public void SelectCard(BoardCard bcard)
        {
            int player_id = GameClient.Get().GetPlayerID();
            bool yourturn = GameClient.Get().IsYourTurn();
            bool yourcard = bcard.GetCard().player_id == player_id;
            Game gdata = GameClient.Get().GetGameData();

            if (gdata.selector == SelectorType.SelectTarget && player_id == gdata.selector_player)
            {
                //Target selector, select this card
                GameClient.Get().SelectCard(bcard.GetCard());
            }
            else if (gdata.state == GameState.Play && gdata.selector == SelectorType.None && yourcard && yourturn)
            {
                //Start dragging card
                selected_card = bcard;
            }
        }

        public void SelectCardRight(BoardCard card)
        {
            bool yourturn = GameClient.Get().IsYourTurn();
            bool yourcard = card.GetCard().player_id == GameClient.Get().GetPlayerID();
            if (yourcard && yourturn && !Input.GetMouseButton(0))
            {
                //Nothing on right-click
            }
        }

        private void ReleaseClick()
        {
            bool yourturn = GameClient.Get().IsYourTurn();
            Game gdata = GameClient.Get().GetGameData();

            if (yourturn && selected_card != null)
            {
                Vector3 wpos = GameBoard.Get().RaycastMouseBoard();
                BoardSlot tslot = BoardSlot.GetNearest(wpos, 2f);
                Card target = tslot ? gdata.GetSlotCard(tslot.GetSlot()) : null;
                AbilityButton ability = AbilityButton.GetHover(wpos, 1f);
                PlayerAttackZone zone = PlayerAttackZone.Get(true);
                float zone_dist = Vector3.Distance(zone.transform.position, wpos);
                
                if (ability != null && ability.IsVisible())
                {
                    ability.OnClick();
                }
                else if (zone_dist < 1f)
                {
                    if (selected_card.GetCard().exhausted)
                        WarningText.ShowExhausted();
                    else
                        GameClient.Get().AttackPlayer(selected_card.GetCard(), zone.GetPlayer());
                }
                else if (target != null && target.uid != selected_card.GetCardUID())
                {
                    if(selected_card.GetCard().exhausted)
                        WarningText.ShowExhausted();
                    else
                        GameClient.Get().AttackTarget(selected_card.GetCard(), target);
                }
                else if (tslot != null)
                {
                    GameClient.Get().Move(selected_card.GetCard(), tslot.GetSlot());
                }

                UnselectAll();
            }
        }

        public void UnselectAll()
        {
            selected_card = null;
        }

        public BoardCard GetSelected()
        {
            return selected_card;
        }

        public static PlayerControls Get()
        {
            return instance;
        }
    }
}