using TcgEngine.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// Main script for the main menu scene
    /// </summary>

    public class MainMenu : MonoBehaviour
    {
        public AudioClip music;
        public AudioClip ambience;

        [Header("Player UI")]
        public Text username_txt;
        public Text credits_txt;
        public AvatarUI avatar;
        public GameObject loader;

        [Header("UI")]
        public Text version_text;
        public DeckSelector deck_selector;
        public DeckDisplay deck_preview;

        private bool starting = false;

        private static MainMenu instance;

        void Awake()
        {
            instance = this;

            //Set default settings
            GameClient.game_settings = GameSettings.Default;
        }

        private void Start()
        {
            BlackPanel.Get().Show(true);
            BlackPanel.Get().Hide();
            AudioTool.Get().PlayMusic("music", music);
            AudioTool.Get().PlaySFX("ambience", ambience, 0.5f, true, true);

            version_text.text = "Version " + Application.version;
            deck_selector.onChange += OnChangeDeck;

            if (Authenticator.Get().IsConnected())
                AfterLogin();
            else
                RefreshLogin();
        }

        void Update()
        {
            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                credits_txt.text = GameUI.FormatNumber(udata.coins);
            }

            bool matchmaking = GameClientMatchmaker.Get().IsMatchmaking();
            if (loader.activeSelf != matchmaking)
                loader.SetActive(matchmaking);
            if (MatchmakingPanel.Get().IsVisible() != matchmaking)
                MatchmakingPanel.Get().SetVisible(matchmaking);
        }

        private async void RefreshLogin()
        {
            bool success = await Authenticator.Get().RefreshLogin();
            if (success)
                AfterLogin();
            else
                SceneNav.GoTo("LoginMenu");
        }

        private void AfterLogin()
        {
            BlackPanel.Get().Hide();

            //Events
            GameClientMatchmaker matchmaker = GameClientMatchmaker.Get();
            matchmaker.onMatchingComplete += OnMatchmakingDone;
            matchmaker.onMatchList += OnReceiveObserver;

            //Deck
            GameClient.player_settings.deck = PlayerPrefs.GetString("tcg_deck_" + Authenticator.Get().Username, "");

            //UserData
            RefreshUserData();

            //Friend list
            //FriendPanel.Get().Show();
        }

        public async void RefreshUserData()
        {
            UserData user = await Authenticator.Get().LoadUserData();
            if (user != null)
            {
                username_txt.text = user.username;
                credits_txt.text = GameUI.FormatNumber(user.coins);
                
                AvatarData avatar = AvatarData.Get(user.avatar);
                this.avatar.SetAvatar(avatar);

                //Decks
                RefreshDeckList();
            }
        }

        public void RefreshDeckList()
        {
            deck_selector.RefreshDeckList();
            deck_selector.SelectDeck(GameClient.player_settings.deck);
            RefreshDeck(deck_selector.GetDeck());
        }

        private void RefreshDeck(string tid)
        {
            UserData user = Authenticator.Get().UserData;
            UserDeckData udeck = user.GetDeck(tid);
            DeckData ddeck = DeckData.Get(tid);
            if (udeck != null)
                deck_preview.SetDeck(udeck);
            else if(ddeck != null)
                deck_preview.SetDeck(ddeck);
            else
                deck_preview.Clear();
        }

        private void OnChangeDeck(string tid)
        {
            GameClient.player_settings.deck = tid;
            PlayerPrefs.SetString("tcg_deck_" + Authenticator.Get().Username, tid);
            RefreshDeck(tid);
        }

        private void OnMatchmakingDone(MatchmakingResult result)
        {
            if (result == null)
                return;

            if (result.success)
            {
                Debug.Log("Matchmaking found: " + result.success + " " + result.server_url + "/" + result.game_uid);
                StartGame(PlayMode.Multiplayer, GameMode.Ranked, result.server_url, result.game_uid);
            }
            else
            {
                MatchmakingPanel.Get().SetCount(result.players);
            }
        }

        private void OnReceiveObserver(MatchList list)
        {
            MatchListItem target = null;
            foreach (MatchListItem item in list.items)
            {
                if (item.username == GameClient.observe_user)
                    target = item;
            }

            if (target != null)
            {
                StartGame(PlayMode.Observer, target.game_uid);
            }
        }

        public void StartGame(PlayMode mode, string game_uid)
        {
            StartGame(mode, GameMode.Casual, "", game_uid); //Empty server_url will use the default one in NetworkData
        }

        public void StartGame(PlayMode mode, GameMode rank, string server_url, string game_uid)
        {
            if (!starting)
            {
                starting = true;
                GameClient.game_settings.play_mode = mode;
                GameClient.game_settings.game_mode = rank;
                GameClient.game_settings.server_url = server_url;
                GameClient.game_settings.game_uid = game_uid;
                GameClient.game_settings.scene = GameplayData.Get().GetRandomArena();
                GameClientMatchmaker.Get().Disconnect();
                FadeToScene(GameClient.game_settings.GetScene());
            }
        }

        public void StartObserve(string user)
        {
            GameClient.observe_user = user;
            GameClientMatchmaker.Get().StopMatchmaking();
            GameClientMatchmaker.Get().RefreshMatchList(user);
        }

        public void StartChallenge(string user)
        {
            string self = Authenticator.Get().Username;
            if (self == user)
                return; //Cant challenge self

            string key;
            if (self.CompareTo(user) > 0)
                key = self + "-" + user;
            else
                key = user + "-" + self;

            StartMathmaking(key);
        }

        public void StartMathmaking(string group)
        {
            GameClient.player_settings.deck = deck_selector.GetDeck();
            GameClientMatchmaker.Get().StartMatchmaking(group, GameClient.game_settings.nb_players);
        }

        public void OnClickSolo()
        {
            if (!Authenticator.Get().IsConnected())
            {
                FadeToScene("LoginMenu");
                return;
            }

            GameClient.player_settings.deck = deck_selector.GetDeck();
            GameClient.ai_settings.deck = GameplayData.Get().GetRandomAIDeck();
            GameClient.ai_settings.ai_level = GameplayData.Get().ai_level;

            string uid = GameTool.GenerateRandomID();
            StartGame(PlayMode.Solo, uid);
        }

        public void OnClickPvP()
        {
            if (!Authenticator.Get().IsConnected())
            {
                FadeToScene("LoginMenu");
                return;
            }

            StartMathmaking("");
        }

        public void OnClickCancelMatch()
        {
            GameClientMatchmaker.Get().StopMatchmaking();
        }

        public void FadeToScene(string scene)
        {
            StartCoroutine(FadeToRun(scene));
        }

        private IEnumerator FadeToRun(string scene)
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            yield return new WaitForSeconds(1f);
            SceneNav.GoTo(scene);
        }

        public void OnClickLogout()
        {
            TcgNetwork.Get().Disconnect();
            Authenticator.Get().Logout();
            FadeToScene("LoginMenu");
        }

        public void OnClickQuit()
        {
            Application.Quit();
        }

        public static MainMenu Get()
        {
            return instance;
        }
    }
}
