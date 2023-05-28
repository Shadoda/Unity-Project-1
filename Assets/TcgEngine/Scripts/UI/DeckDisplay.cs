using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Can display a deck in the UI
    /// Only shows a few cards and the total amount of cards
    /// </summary>

    public class DeckDisplay : MonoBehaviour
    {
        public Text deck_title;
        public Text card_count;
        public CardUI[] ui_cards;

        private string deck_id;

        void Awake()
        {
            Clear();
        }

        void Update()
        {

        }

        public void Clear()
        {
            if (deck_title != null)
                deck_title.text = "";
            if (card_count != null)
                card_count.text = "";
            foreach (CardUI card in ui_cards)
                card.Hide();
        }

        public void SetDeck(UserDeckData deck)
        {
            Clear();

            if (deck != null)
            {
                deck_id = deck.tid;

                if (deck_title != null)
                    deck_title.text = deck.title;

                if (card_count != null)
                {
                    card_count.text = deck.GetQuantity().ToString() + " / " + GameplayData.Get().deck_size.ToString();
                    card_count.color = deck.GetQuantity() >= GameplayData.Get().deck_size ? Color.white : Color.red;
                }

                List<CardData> cards = new List<CardData>();
                foreach (string tid in deck.cards)
                {
                    string card_id = UserCardData.GetCardId(tid);
                    CardVariant variant = UserCardData.GetCardVariant(tid);
                    CardData icard = CardData.Get(card_id);
                    if (icard != null)
                        cards.Add(icard);
                }

                ShowCards(cards);
            }

            gameObject.SetActive(deck != null);
        }

        public void SetDeck(DeckData deck)
        {
            Clear();

            if (deck != null)
            {
                deck_id = deck.id;

                if (deck_title != null)
                    deck_title.text = deck.title;

                if (card_count != null)
                {
                    card_count.text = deck.GetQuantity().ToString() + " / " + GameplayData.Get().deck_size.ToString();
                    card_count.color = deck.GetQuantity() >= GameplayData.Get().deck_size ? Color.white : Color.red;
                }

                List<CardData> dcards = new List<CardData>();
                dcards.AddRange(deck.cards);

                ShowCards(dcards);
            }

            gameObject.SetActive(deck != null);
        }

        public void ShowCards(List<CardData> cards)
        {
            cards.Sort((CardData a, CardData b) => { return b.mana.CompareTo(a.mana); });

            int index = 0;
            foreach (CardData icard in cards)
            {
                if (index < ui_cards.Length)
                {
                    CardUI card_ui = ui_cards[index];
                    card_ui.SetCard(icard, CardVariant.Normal);
                    index++;
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public string GetDeck()
        {
            return deck_id;
        }
    }
}
