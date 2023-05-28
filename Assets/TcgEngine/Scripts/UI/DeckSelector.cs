using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// Deck selector is a dropdown that let the player select a deck before a match
    /// </summary>

    public class DeckSelector : MonoBehaviour
    {
        public DropdownValue deck_dropdown;

        public UnityAction<string> onChange;

        void Start()
        {
            deck_dropdown.onValueChanged += OnChange;
        }

        void Update()
        {

        }

        public void RefreshDeckList()
        {
            deck_dropdown.ClearOptions();

            //Add standard decks
            foreach (DeckData deck in GameplayData.Get().free_decks)
            {
                deck_dropdown.AddOption(deck.id, deck.title);
            }

            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                foreach (UserDeckData deck in udata.decks)
                {
                    if (deck.IsValid())
                    {
                        deck_dropdown.AddOption(deck.tid, deck.title);
                    }
                }
            }
        }

        private void SelectDeck(UserDeckData deck)
        {
            if (deck != null)
            {
                deck_dropdown.SetValue(deck.tid);
            }
        }

        private void SelectDeck(DeckData deck)
        {
            if (deck != null)
            {
                deck_dropdown.SetValue(deck.id);
            }
        }

        public void SelectDeck(string deck)
        {
            //Make sure deck exists, to prevent assigning invalid deck
            UserData udata = Authenticator.Get().UserData;
            UserDeckData udeck = udata?.GetDeck(deck);
            if (udeck != null)
            {
                deck_dropdown.SetValue(deck);
                SelectDeck(udeck);
                return;
            }

            DeckData adeck = DeckData.Get(deck);
            if(adeck != null)
                SelectDeck(adeck);
        }

        public void Lock()
        {
            deck_dropdown.interactable = false;
        }

        public void Unlock()
        {
            deck_dropdown.interactable = true;
        }

        public void SetLocked(bool locked)
        {
            deck_dropdown.interactable = !locked;
        }

        private void OnChange(int i, string val)
        {
            string value = deck_dropdown.GetSelectedValue();
            onChange?.Invoke(value);
        }

        public string GetDeck()
        {
            return deck_dropdown.GetSelectedValue();
        }
    }
}