using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// CollectionPanel is the panel where players can see all the cards they own
    /// Also the panel where they can use the deckbuilder
    /// </summary>

    public class CollectionPanel : UIPanel
    {
        [Header("Cards")]
        public ScrollRect scroll_rect;
        public RectTransform scroll_content;
        public CardGrid grid_content;
        public GameObject card_prefab;

        [Header("Left Side")]
        public IconButton[] team_filters;
        public Toggle toggle_owned;
        public Toggle toggle_not_owned;
        //public Toggle toggle_foil;
        public Toggle toggle_character;
        public Toggle toggle_spell;
        public Toggle toggle_artifact;
        public Toggle toggle_secret;

        public Toggle toggle_common;
        public Toggle toggle_uncommon;
        public Toggle toggle_rare;
        public Toggle toggle_mythic;

        public Dropdown sort_dropdown;
        public InputField search;

        [Header("Right Side")]
        public UIPanel deck_list_panel;
        public UIPanel card_list_panel;
        public DeckLine[] deck_lines;

        [Header("Deckbuilding")]
        public InputField deck_title;
        public Text deck_quantity;
        public GameObject deck_cards_prefab;
        public RectTransform deck_content;
        public GridLayoutGroup deck_grid;

        private TeamData filter_planet = null;
        private int filter_dropdown = 0;
        private string filter_search = "";

        private List<CollectionCard> card_list = new List<CollectionCard>();
        private List<CollectionCard> all_list = new List<CollectionCard>();
        private List<DeckLine> deck_card_lines = new List<DeckLine>();

        private string current_deck_tid;
        private Dictionary<CardData, int> deck_cards = new Dictionary<CardData, int>();
        private bool editing_deck = false;
        private bool saving = false;
        private bool spawned = false;
        private bool update_grid = false;
        private float update_grid_timer = 0f;

        private static CollectionPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            //Delete grid content
            for (int i = 0; i < grid_content.transform.childCount; i++)
                Destroy(grid_content.transform.GetChild(i).gameObject);
            for (int i = 0; i < deck_grid.transform.childCount; i++)
                Destroy(deck_grid.transform.GetChild(i).gameObject);

            foreach (DeckLine line in deck_lines)
                line.onClick += OnClickDeckLine;
            foreach (DeckLine line in deck_lines)
                line.onClickDelete += OnClickDeckDelete;

            foreach (IconButton button in team_filters)
                button.onClick += OnClickPlanet;

            //if (TheGame.IsMobile())
            //    MobileResize();
        }

        protected override void Start()
        {
            base.Start();

        }

        protected override void Update()
        {
            base.Update();

        }

        private void LateUpdate()
        {
            //Resize grid
            update_grid_timer += Time.deltaTime;
            if (update_grid && update_grid_timer > 0.2f)
            {
                grid_content.GetColumnAndRow(out int rows, out int cols);
                if (cols > 0)
                {
                    float row_height = grid_content.GetGrid().cellSize.y + grid_content.GetGrid().spacing.y;
                    float height = rows * row_height;
                    scroll_content.sizeDelta = new Vector2(scroll_content.sizeDelta.x, height + 100);
                    update_grid = false;
                }
            }
        }

        public async void ReloadUserCards()
        {
            await Authenticator.Get().LoadUserData();
            RefreshCards();
        }

        public async void ReloadUserDecks()
        {
            await Authenticator.Get().LoadUserData();
            MainMenu.Get().RefreshDeckList();
            RefreshDeckList();
        }

        private void RefreshAll()
        {
            RefreshFilters();
            RefreshCards();
            RefreshDeckList();
            RefreshStarterDeck();
        }

        private void RefreshFilters()
        {
            search.text = "";
            sort_dropdown.value = 0;
            foreach (IconButton button in team_filters)
                button.Deactivate();

            filter_planet = null;
            filter_dropdown = 0;
            filter_search = "";
        }

        private void SpawnCards()
        {
            spawned = true;
            foreach (CollectionCard card in all_list)
                Destroy(card.gameObject);
            all_list.Clear();

            foreach (CardData card in CardData.GetAll())
            {
                GameObject nCard = Instantiate(card_prefab, grid_content.transform);
                CollectionCard dCard = nCard.GetComponent<CollectionCard>();
                dCard.SetCard(card, CardVariant.Normal, 0);
                dCard.onClick += OnClickCard;
                dCard.onClickRight += OnClickCardRight;
                all_list.Add(dCard);
                nCard.SetActive(false);
            }
        }

        public void RefreshCards()
        {
            if (!spawned)
                SpawnCards();

            foreach (CollectionCard card in all_list)
                card.gameObject.SetActive(false);
            card_list.Clear();

            bool is_test = Authenticator.Get().IsTest();
            UserData udata = Authenticator.Get().UserData;

            List<CardDataQ> all_cards = new List<CardDataQ>();
            List<CardDataQ> shown_cards = new List<CardDataQ>();

            foreach (CardData icard in CardData.GetAll())
            {
                CardDataQ card = new CardDataQ();
                card.card = icard;
                card.variant = CardVariant.Normal;
                card.quantity = udata.GetCardQuantity(UserCardData.GetTid(icard.id, CardVariant.Normal));
                all_cards.Add(card);
            }

            if (filter_dropdown == 0) //Name
                all_cards.Sort((CardDataQ a, CardDataQ b) => { return a.card.title.CompareTo(b.card.title); });
            if (filter_dropdown == 1) //Attack
                all_cards.Sort((CardDataQ a, CardDataQ b) => { return b.card.attack == a.card.attack ? b.card.hp.CompareTo(a.card.hp) : b.card.attack.CompareTo(a.card.attack); });
            if (filter_dropdown == 2) //hp
                all_cards.Sort((CardDataQ a, CardDataQ b) => { return b.card.hp == a.card.hp ? b.card.attack.CompareTo(a.card.attack) : b.card.hp.CompareTo(a.card.hp); });
            if (filter_dropdown == 3) //Cost
                all_cards.Sort((CardDataQ a, CardDataQ b) => { return b.card.mana == a.card.mana ? a.card.title.CompareTo(b.card.title) : a.card.mana.CompareTo(b.card.mana); });
            
            foreach (CardDataQ card in all_cards)
            {
                if (card.card.deckbuilding)
                {
                    CardData icard = card.card;
                    if (filter_planet == null || filter_planet == icard.team)
                    {
                        bool owned = card.quantity > 0 || is_test;
                        RarityData rarity = icard.rarity;
                        CardType type = icard.type;

                        bool owned_check = (owned && toggle_owned.isOn)
                            || (!owned && toggle_not_owned.isOn)
                            || toggle_owned.isOn == toggle_not_owned.isOn;

                        bool type_check = (type == CardType.Character && toggle_character.isOn)
                            || (type == CardType.Spell && toggle_spell.isOn)
                            || (type == CardType.Artifact && toggle_artifact.isOn)
                            || (type == CardType.Secret && toggle_secret.isOn)
                            || (!toggle_character.isOn && !toggle_spell.isOn && !toggle_artifact.isOn && !toggle_secret.isOn);

                        bool rarity_check = (rarity.rank == 1 && toggle_common.isOn)
                            || (rarity.rank == 2 && toggle_uncommon.isOn)
                            || (rarity.rank == 3 && toggle_rare.isOn)
                            || (rarity.rank == 4 && toggle_mythic.isOn)
                            || (!toggle_common.isOn && !toggle_uncommon.isOn && !toggle_rare.isOn && !toggle_mythic.isOn);

                        string search = filter_search.ToLower();
                        bool search_check = string.IsNullOrWhiteSpace(search)
                            || icard.id.Contains(search)
                            || icard.title.ToLower().Contains(search)
                            || icard.GetText().ToLower().Contains(search);

                        if (owned_check && type_check && rarity_check && search_check)
                        {
                            shown_cards.Add(card);
                        }
                    }
                }
            }

            int index = 0;
            foreach (CardDataQ qcard in shown_cards)
            {
                if (index < all_list.Count)
                {
                    CollectionCard dcard = all_list[index];
                    int quantity = udata.GetCardQuantity(UserCardData.GetTid(qcard.card.id, CardVariant.Normal));
                    dcard.SetCard(qcard.card, qcard.variant, quantity);
                    card_list.Add(dcard);
                    dcard.gameObject.SetActive(true);
                    index++;
                }
            }

            update_grid = true;
            update_grid_timer = 0f;
            scroll_rect.verticalNormalizedPosition = 1f;
            RefreshCardsOpacity();
        }

        private void RefreshCardsOpacity()
        {
            UserData udata = Authenticator.Get().UserData;
            foreach (CollectionCard card in card_list)
            {
                CardData icard = card.GetCard();
                bool owned = IsCardOwned(udata, icard, card.GetVariant(), 1);
                card.SetGrayscale(!owned);
            }
        }

        private void RefreshDeckList()
        {
            foreach (DeckLine line in deck_lines)
                line.Hide();
            deck_list_panel.Show();
            card_list_panel.Hide();
            deck_cards.Clear();
            editing_deck = false;

            UserData udata = Authenticator.Get().UserData;
            if (udata == null)
                return;

            int index = 0;
            foreach (UserDeckData deck in udata.decks)
            {
                if (index < deck_lines.Length)
                {
                    DeckLine line = deck_lines[index];
                    line.SetLine(udata, deck);
                }
                index++;
            }

            if (index < deck_lines.Length)
            {
                DeckLine line = deck_lines[index];
                line.SetLine("+");
            }
            RefreshCardsOpacity();
        }

        private void RefreshDeck(UserDeckData deck)
        {
            deck_title.text = "Deck Name";
            current_deck_tid = GameTool.GenerateRandomID(7);
            deck_cards.Clear();
            saving = false;

            if (deck != null)
            {
                deck_title.text = deck.title;
                current_deck_tid = deck.tid;

                for (int i = 0; i < deck.cards.Length; i++)
                {
                    string cid = UserCardData.GetCardId(deck.cards[i]);
                    CardVariant variant = UserCardData.GetCardVariant(deck.cards[i]);
                    CardData card = CardData.Get(cid);
                    if (card != null)
                    {
                        AddDeckCard(card);
                    }
                }
            }

            editing_deck = true;
            RefreshDeckCards();
        }

        private void RefreshDeckCards()
        {
            foreach (DeckLine line in deck_card_lines)
                line.Hide();
            deck_list_panel.Hide();
            card_list_panel.Show();

            List<CardDataQ> list = new List<CardDataQ>();
            foreach (KeyValuePair<CardData, int> pair in deck_cards)
            {
                CardDataQ acard = new CardDataQ();
                acard.card = pair.Key;
                acard.quantity = pair.Value;
                list.Add(acard);
            }
            list.Sort((CardDataQ a, CardDataQ b) => { return a.card.title.CompareTo(b.card.title); });

            UserData udata = Authenticator.Get().UserData;
            int index = 0;
            int count = 0;
            foreach (CardDataQ card in list)
            {
                if (index >= deck_card_lines.Count)
                    CreateDeckCard();

                if (index < deck_card_lines.Count)
                {
                    DeckLine line = deck_card_lines[index];
                    if (line != null)
                    {
                        CardVariant variant = CardVariant.Normal;
                        //CardVariant variant = card_variants.ContainsKey(card) ? card_variants[card] : CardVariant.Normal;
                        //string tid = UserCardData.GetTid(card.id, variant);
                        line.SetLine(card.card, variant, card.quantity, !IsCardOwned(udata, card.card, variant, card.quantity));
                        count += card.quantity;
                    }
                }
                index++;
            }

            deck_quantity.text = count + "/" + GameplayData.Get().deck_size;
            deck_quantity.color = count >= GameplayData.Get().deck_size ? Color.white : Color.red;

            RefreshCardsOpacity();
        }

        private void CreateDeckCard()
        {
            GameObject deck_line = Instantiate(deck_cards_prefab, deck_grid.transform);
            DeckLine line = deck_line.GetComponent<DeckLine>();
            deck_card_lines.Add(line);
            float height = deck_card_lines.Count * 70f + 20f;
            deck_content.sizeDelta = new Vector2(deck_content.sizeDelta.x, height);
            line.onClick += OnClickCardLine;
            line.onClickRight += OnRightClickCardLine;
        }

        private void AddDeckCard(CardData card)
        {
            if (deck_cards.ContainsKey(card))
                deck_cards[card] += 1;
            else
                deck_cards[card] = 1;
        }

        private void RemoveDeckCard(CardData card)
        {
            if (deck_cards.ContainsKey(card))
                deck_cards[card] -= 1;
            if (deck_cards[card] <= 0)
                deck_cards.Remove(card);
        }

        private void RefreshStarterDeck()
        {
            UserData udata = Authenticator.Get().UserData;
            if (Authenticator.Get().IsApi() && udata.cards.Length == 0 && udata.decks.Length == 0)
            {
                StarterDeckPanel.Get().Show();
            }
        }

        private void SaveDeck()
        {
            UserData udata = Authenticator.Get().UserData;
            UserDeckData udeck = new UserDeckData();
            udeck.tid = current_deck_tid;
            udeck.title = deck_title.text;
            saving = true;

            List<string> card_list = new List<string>();
            foreach (KeyValuePair<CardData, int> pair in deck_cards)
            {
                if (pair.Key != null)
                {
                    CardVariant variant = CardVariant.Normal;
                    for (int i = 0; i < pair.Value; i++)
                        card_list.Add(UserCardData.GetTid(pair.Key.id, variant));
                }
            }
            udeck.cards = card_list.ToArray();

            if (Authenticator.Get().IsTest())
                SaveDeckTest(udata, udeck);

            if (Authenticator.Get().IsApi())
                SaveDeckAPI(udata, udeck);
        }

        private async void SaveDeckTest(UserData udata, UserDeckData udeck)
        {
            udata.SetDeck(udeck);
            await Authenticator.Get().SaveUserData();
            ReloadUserDecks();
        }

        private async void SaveDeckAPI(UserData udata, UserDeckData udeck)
        {
            string url = ApiClient.ServerURL + "/users/deck/" + udeck.tid;
            string jdata = ApiTool.ToJson(udeck);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, jdata);
            ListResponse<UserDeckData> decks = ApiTool.JsonToArray<UserDeckData>(res.data);
            saving = res.success;

            if (res.success && decks.list != null)
            {
                udata.decks = decks.list;
                await Authenticator.Get().SaveUserData();
                ReloadUserDecks();
            }
        }

        private async void DeleteDeck(string deck_tid)
        {
            UserData udata = Authenticator.Get().UserData;
            UserDeckData udeck = udata.GetDeck(deck_tid);
            List<UserDeckData> decks = new List<UserDeckData>(udata.decks);
            decks.Remove(udeck);
            udata.decks = decks.ToArray();

            if (Authenticator.Get().IsApi())
            {
                string url = ApiClient.ServerURL + "/users/deck/" + deck_tid;
                await ApiClient.Get().SendRequest(url, "DELETE", "");
            }

            await Authenticator.Get().SaveUserData();
            ReloadUserDecks();
        }

        private bool IsCardOwned(UserData udata, CardData card, CardVariant variant, int quantity)
        {
            bool is_test = Authenticator.Get().IsTest();
            string tid = UserCardData.GetTid(card.id, variant);
            return udata.GetCardQuantity(tid) >= quantity || is_test;
        }

        public void OnClickPlanet(IconButton button)
        {
            filter_planet = null;
            if (button.IsActive())
            {
                foreach (TeamData team in TeamData.GetAll())
                {
                    if (button.value == team.id)
                        filter_planet = team;
                }
            }
            RefreshCards();
        }

        public void OnChangeToggle()
        {
            RefreshCards();
        }

        public void OnChangeDropdown()
        {
            filter_dropdown = sort_dropdown.value;
            RefreshCards();
        }

        public void OnChangeSearch()
        {
            filter_search = search.text;
            RefreshCards();
        }

        public void OnClickDeckBack()
        {
            RefreshDeckList();
        }

        public void OnClickDeckLine(DeckLine line)
        {
            if (line.IsHidden())
                return;
            UserDeckData deck = line.GetUserDeck();
            RefreshDeck(deck);
        }

        public void OnClickDeckDelete(DeckLine line)
        {
            if (line.IsHidden())
                return;
            UserDeckData deck = line.GetUserDeck();
            if (deck != null)
            {
                DeleteDeck(deck.tid);
            }
        }

        public void OnClickDeleteDeck()
        {
            if (editing_deck && !string.IsNullOrEmpty(current_deck_tid))
            {
                DeleteDeck(current_deck_tid);
            }
        }

        private void OnClickCardLine(DeckLine line)
        {
            CardData card = line.GetCard();
            if (card != null)
            {
                RemoveDeckCard(card);
            }

            RefreshDeckCards();
        }

        private void OnRightClickCardLine(DeckLine line)
        {
            CardData icard = line.GetCard();
            if (icard != null)
                CardZoomPanel.Get().ShowCard(icard, line.GetVariant());
        }

        public void OnClickCard(CardUI card)
        {
            if (!editing_deck)
            {
                CardZoomPanel.Get().ShowCard(card.GetCard(), card.GetVariant());
                return;
            }

            CardData icard = card.GetCard();
            if (icard != null)
            {
                int in_deck = CountDeckCards(icard);
                UserData udata = Authenticator.Get().UserData;

                bool owner = IsCardOwned(udata, card.GetCard(), card.GetVariant(), in_deck + 1);
                bool deck_limit = in_deck < GameplayData.Get().deck_duplicate_max;

                if (owner && deck_limit)
                {
                    AddDeckCard(icard);
                    RefreshDeckCards();
                }
            }
        }

        public void OnClickCardRight(CardUI card)
        {
            CardZoomPanel.Get().ShowCard(card.GetCard(), card.GetVariant());
        }

        public void OnClickSaveDeck()
        {
            if (saving)
                return;

            SaveDeck();
        }

        public int CountDeckCards(CardData card)
        {
            if (deck_cards.ContainsKey(card))
                return deck_cards[card];
            return 0;
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshAll();
        }

        public static CollectionPanel Get()
        {
            return instance;
        }
    }

    public struct CardDataQ
    {
        public CardData card;
        public CardVariant variant;
        public int quantity;
    }
}