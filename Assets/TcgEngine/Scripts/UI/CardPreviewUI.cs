using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// In the game scene, the CardPreviewUI is what shows the card in big with extra info when hovering a card
    /// </summary>

    public class CardPreviewUI : MonoBehaviour
    {
        public UIPanel ui_panel;
        public CardUI card_ui;
        public Text desc;
        public float hover_delay_board = 0.7f;
        public float hover_delay_hand = 0.4f;
        public float hover_delay_mobile = 0.1f;

        public RectTransform[] side_rows;
        public StatusLine[] status_lines;

        private float preview_timer = 0f;
        private Vector2[] start_pos;
        private string last_card_uid;

        private void Start()
        {
            start_pos = new Vector2[side_rows.Length];
            for (int i = 0; i < side_rows.Length; i++)
            {
                start_pos[i] = side_rows[i].anchoredPosition;
            }
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            card_ui.Hide();

            foreach (StatusLine line in status_lines)
                line.Hide();

            HandCard hcard = HandCard.GetFocus();
            BoardCard bcard = BoardCard.GetFocus();
            Card histcard = TurnHistoryLine.GetHoverCard();
            PlayerControls controls = PlayerControls.Get();

            float delay = hcard != null ? hover_delay_hand : hover_delay_board;
            if (GameTool.IsMobile())
                delay = hover_delay_mobile;

            Card pcard = hcard != null ? hcard?.GetCard() : bcard?.GetCard();
            if (pcard == null)
                pcard = histcard;

            bool hover_only = !Input.GetMouseButton(0) && !HandCardArea.Get().IsDragging();
            bool should_show_preview = hover_only && !GameUI.IsUIOpened() && pcard != null;

            if (pcard != null && last_card_uid != pcard.uid)
                should_show_preview = false;

            if (should_show_preview)
                preview_timer += Time.deltaTime;
            else
                preview_timer = 0f;

            bool show_preview = should_show_preview && preview_timer >= delay;
            ui_panel.SetVisible(show_preview);
            last_card_uid = pcard != null ? pcard.uid : "";

            if (show_preview)
            {
                CardData icard = CardData.Get(pcard.card_id);
                card_ui.SetCard(icard, pcard.variant);

                string cdesc = icard.GetDesc();
                string adesc = icard.GetAbilitiesDesc();
                if (!string.IsNullOrWhiteSpace(cdesc))
                    this.desc.text = cdesc + "\n\n" + adesc;
                else
                    this.desc.text = adesc;

                //Status
                int index = 0;
                foreach (CardStatus status in pcard.GetAllStatus())
                {
                    if (index < status_lines.Length)
                    {
                        StatusData istatus = StatusData.Get(status.type);
                        if (istatus != null && !string.IsNullOrWhiteSpace(istatus.desc))
                        {
                            int ival = Mathf.Max(status.value, Mathf.CeilToInt(status.duration / 2f));
                            status_lines[index].SetLine(istatus, ival);
                            index++;
                        }
                    }
                }
            }

        }
    }
}
