using TcgEngine.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// Line FX that appear when dragin a board card to attack, top part of the fx
    /// </summary>

    public class MouseLineTopFX : MonoBehaviour
    {
        public GameObject fx;

        void Start()
        {

        }

        void Update()
        {
            PlayerControls controls = PlayerControls.Get();
            BoardCard bcard = controls.GetSelected();

            bool visible = false;
            if (bcard != null)
            {
                Game data = GameClient.Get().GetGameData();
                Card card = bcard.GetCard();
                Player player = GameClient.Get().GetPlayer();

                if (data.IsPlayerActionTurn(player) && card.CanDoAnyAction()) 
                {
                    visible = true;
                }
            }

            HandCard drag = HandCard.GetDrag();
            if (drag != null)
            {
                visible = drag.GetCardData().IsRequireTarget();
            }

            if (fx.activeSelf != visible)
                fx.SetActive(visible);

            if (visible)
            {
                Vector3 dest = GameBoard.Get().RaycastMouseBoard();
                transform.position = dest;
            }
        }
    }
}
