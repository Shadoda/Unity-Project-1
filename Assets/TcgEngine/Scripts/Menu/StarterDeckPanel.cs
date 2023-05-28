using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Selection of your starter deck
    /// Will only appear in the main menu when in API mode with a new account
    /// </summary>

    public class StarterDeckPanel : UIPanel
    {
        public DeckDisplay[] decks;

        public Text error;

        private static StarterDeckPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        private void RefreshPanel()
        {
            int index = 0;
            foreach (DeckData deck in GameplayData.Get().starter_decks)
            {
                if (index < decks.Length)
                {
                    DeckDisplay display = decks[index];
                    display.SetDeck(deck);
                    index++;
                }
            }
        }

        private async void ChooseDeck(string deck_id)
        {
            RewardGainRequest req = new RewardGainRequest();
            req.reward = deck_id;

            if (error != null)
                error.text = "";

            string url = ApiClient.ServerURL + "/users/rewards/gain/" + ApiClient.Get().UserID;
            string json = ApiTool.ToJson(req);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            if (res.success)
            {
                CollectionPanel.Get().ReloadUserDecks();
                Hide();
            }
            else
            {
                if (error != null)
                    error.text = res.error;
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            if(error != null)
                error.text = "";
            RefreshPanel();
        }

        public void OnClickDeck(int index)
        {
            if (index < decks.Length)
            {
                DeckDisplay display = decks[index];
                string deck = display.GetDeck();
                ChooseDeck(deck);
            }
        }

        public static StarterDeckPanel Get()
        {
            return instance;
        }
    }
}
