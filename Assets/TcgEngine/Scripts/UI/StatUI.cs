using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Shows a custom card stats, add it to the array of a CardUI
    /// </summary>

    public class StatUI : MonoBehaviour
    {
        public TraitData trait;
        public Image bg;
        public Text text;

        void Start()
        {

        }

        public void SetCard(Card card)
        {
            int val = card.GetStatValue(trait.id);
            bg.enabled = val > 0;
            text.text = val.ToString();
        }

        public void SetCard(CardData card)
        {
            int val = card.GetStat(trait.id);
            bg.enabled = val > 0;
            text.text = val.ToString();
        }
    }
}
