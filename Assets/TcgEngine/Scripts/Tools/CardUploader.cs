using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine
{
    /// <summary>
    /// Use this tool to upload your cards and packs to the Mongo Database (it will overwrite existing data)
    /// </summary>

    public class CardUploader : MonoBehaviour
    {
        public string username = "admin";

        [Header("References")]
        public InputField username_txt;
        public InputField password_txt;
        public Text msg_text;

        void Start()
        {
            username_txt.text = username;
            msg_text.text = "";
        }

        private async void Login()
        {
            LoginResponse res = await ApiClient.Get().Login(username_txt.text, password_txt.text);
            if (res.success && res.permission_level >= 10)
            {
                UploadAll();
            }
            else
            {
                ShowText("Admin Login Failed");
            }
        }

        private async void UploadAll()
        {
            //Delete previous data
            ShowText("Deleting previous data...");
            await DeleteAllPacks();
            await DeleteAllCards();
            await DeleteAllDecks();

            //Packs
            List<PackData> packs = PackData.GetAll();
            for (int i = 0; i < packs.Count; i++)
            {
                PackData pack = packs[i];
                if (pack.available)
                {
                    ShowText("Uploading: " + pack.id);
                    UploadPack(pack);
                    await Task.Delay(100);
                }
            }

            //Cards
            List<CardData> cards = CardData.GetAll();
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card.deckbuilding)
                {
                    ShowText("Uploading: " + card.id);
                    UploadCard(card);
                    await Task.Delay(100);
                }
            }

            //Starter Decks
            DeckData[] decks = GameplayData.Get().starter_decks;
            for (int i = 0; i < decks.Length; i++)
            {
                DeckData deck = decks[i];
                ShowText("Uploading: " + deck.id);
                UploadDeck(deck);
                UploadDeckReward(deck);
                await Task.Delay(100);
            }

            ShowText("Completed!");
            ApiClient.Get().Logout();
        }

        private async Task DeleteAllPacks()
        {
            string url = ApiClient.ServerURL + "/packs";
            await ApiClient.Get().SendRequest(url, WebRequest.METHOD_DELETE);
        }

        private async Task DeleteAllCards()
        {
            string url = ApiClient.ServerURL + "/cards";
            await ApiClient.Get().SendRequest(url, WebRequest.METHOD_DELETE);
        }

        private async Task DeleteAllDecks()
        {
            string url = ApiClient.ServerURL + "/decks";
            await ApiClient.Get().SendRequest(url, WebRequest.METHOD_DELETE);
        }

        private async void UploadPack(PackData pack)
        {
            PackAddRequest req = new PackAddRequest();
            req.tid = pack.id;
            req.cards = pack.cards;
            req.cost = pack.cost;
            req.rarities_1st = pack.rarities_1st;
            req.rarities = pack.rarities;

            string url = ApiClient.ServerURL + "/packs/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private async void UploadCard(CardData card)
        {
            CardAddRequest req = new CardAddRequest();
            req.tid = card.id;
            req.type = card.GetTypeId();
            req.team = card.team.id;
            req.rarity = card.rarity.rank;
            req.mana = card.mana;
            req.attack = card.attack;
            req.hp = card.hp;
            req.cost = card.cost;
            req.packs = new string[card.packs.Length];

            for (int i = 0; i < req.packs.Length; i++)
            {
                req.packs[i] = card.packs[i].id;
            }

            string url = ApiClient.ServerURL + "/cards/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private async void UploadDeckReward(DeckData deck)
        {
            RewardAddRequest req = new RewardAddRequest();
            req.tid = deck.id;
            req.group = "starter_deck";
            req.decks = new string[1] { deck.id };

            string url = ApiClient.ServerURL + "/rewards/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private async void UploadDeck(DeckData deck)
        {
            UserDeckData req = new UserDeckData();
            req.tid = deck.id;
            req.title = deck.title;
            req.cards = new string[deck.cards.Length];

            for (int i = 0; i < req.cards.Length; i++)
            {
                req.cards[i] = deck.cards[i].id;
            }

            string url = ApiClient.ServerURL + "/decks/add";
            string json = ApiTool.ToJson(req);
            await ApiClient.Get().SendPostRequest(url, json);
        }

        private void ShowText(string txt)
        {
            msg_text.text = txt;
            Debug.Log(txt);
        }

        public void OnClickStart()
        {
            msg_text.text = "";
            Login();
        }
    }

    [System.Serializable]
    public class CardAddRequest
    {
        public string tid;
        public string type;
        public string team;
        public int rarity;
        public int mana;
        public int attack;
        public int hp;
        public int cost;
        public string[] packs;
    }

    [System.Serializable]
    public class PackAddRequest
    {
        public string tid;
        public int cards;
        public int cost;
        public int[] rarities_1st;
        public int[] rarities;
    }

    [System.Serializable]
    public class RewardAddRequest
    {
        public string tid;
        public string group;
        public int coins;
        public int xp;
        public string[] packs;
        public string[] cards;
        public string[] decks;
    }
}
