using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using System.Threading.Tasks;

namespace TcgEngine.Client
{
    /// <summary>
    /// Main script for the client-side of the game, should be in game scene only
    /// Will connect to server, then connect to the game on that server (with uid) and then will send game settings
    /// During the game, will send all actions performed by the player and receive game refreshes
    /// </summary>

    public class GameClient : MonoBehaviour
    {
        //--- These settings are set in the menu scene and when the game start will be sent to server

        public static GameSettings game_settings = GameSettings.Default;
        public static PlayerSettings player_settings = PlayerSettings.Default;
        public static PlayerSettings ai_settings = PlayerSettings.DefaultAI;
        public static string observe_user = null; //Which user should it observe, null if not an obs

        //-----

        public UnityAction onConnectServer;
        public UnityAction onConnectGame;
        public UnityAction onGameStart;
        public UnityAction<int> onGameEnd;              //winner player_id
        public UnityAction<int> onNewTurn;              //current player_id
        public UnityAction<Card, Slot> onCardPlayed;
        public UnityAction<Card, Slot> onCardMoved;
        public UnityAction<Slot> onCardSummoned;
        public UnityAction<Card> onCardTransformed;
        public UnityAction<Card> onCardDiscarded;

        public UnityAction<AbilityData, Card> onAbilityStart;
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;      //Ability, Caster, Target
        public UnityAction<AbilityData, Card, Player> onAbilityTargetPlayer;
        public UnityAction<AbilityData, Card, Slot> onAbilityTargetSlot;
        public UnityAction<AbilityData, Card> onAbilityEnd;

        public UnityAction<Card, Card> onAttackStart;   //Attacker, Defender
        public UnityAction<Card, Card> onAttackEnd;     //Attacker, Defender
        public UnityAction<Card, Player> onAttackPlayerStart;
        public UnityAction<Card, Player> onAttackPlayerEnd;

        public UnityAction<Card, Card> onSecret;    //Secret, Triggerer

        public UnityAction<int, string> onChatMsg;  //player_id, msg
        public UnityAction< string> onServerMsg;  //msg
        public UnityAction onRefreshAll;

        private int player_id = 0; //Player playing on this device;
        private Game game_data;

        private bool observe_mode = false;
        private int observe_player_id = 0;
        private float timer = 0f;


        private Dictionary<ushort, RefreshEvent> registered_commands = new Dictionary<ushort, RefreshEvent>();

        private static GameClient _instance;

        protected virtual void Awake()
        {
            _instance = this;
            Application.targetFrameRate = 60;
        }

        protected virtual void Start()
        {
            RegisterRefresh(GameAction.Connected, OnConnectedToGame);
            RegisterRefresh(GameAction.GameStart, OnGameStart);
            RegisterRefresh(GameAction.GameEnd, OnGameEnd);
            RegisterRefresh(GameAction.NewTurn, OnNewTurn);
            RegisterRefresh(GameAction.CardPlayed, OnCardPlayed);
            RegisterRefresh(GameAction.CardSummoned, OnCardSummoned);
            RegisterRefresh(GameAction.CardTransformed, OnCardTransformed);
            RegisterRefresh(GameAction.CardDiscarded, OnCardDiscarded);
            RegisterRefresh(GameAction.CardMoved, OnCardMoved);

            RegisterRefresh(GameAction.AttackStart, OnAttackStart);
            RegisterRefresh(GameAction.AttackEnd, OnAttackEnd);
            RegisterRefresh(GameAction.AttackPlayerStart, OnAttackPlayerStart);
            RegisterRefresh(GameAction.AttackPlayerEnd, OnAttackPlayerEnd);

            RegisterRefresh(GameAction.AbilityTrigger, OnAbilityTrigger);
            RegisterRefresh(GameAction.AbilityTargetCard, OnAbilityTargetCard);
            RegisterRefresh(GameAction.AbilityTargetPlayer, OnAbilityTargetPlayer);
            RegisterRefresh(GameAction.AbilityTargetSlot, OnAbilityTargetSlot);
            RegisterRefresh(GameAction.AbilityEnd, OnAbilityAfter);

            RegisterRefresh(GameAction.SecretResolved, OnSecret);

            RegisterRefresh(GameAction.ChatMessage, OnChat);
            RegisterRefresh(GameAction.ServerMessage, OnServerMsg);
            RegisterRefresh(GameAction.RefreshAll, OnRefreshAll);

            TcgNetwork.Get().onConnect += OnConnectServer;
            TcgNetwork.Get().Messaging.ListenMsg("refresh", OnReceiveRefresh);

            ConnectToAPI();
            ConnectToServer();
        }

        protected virtual void OnDestroy()
        {
            TcgNetwork.Get().onConnect -= OnConnectServer;
            TcgNetwork.Get().Messaging.UnListenMsg("refresh");
        }

        protected virtual void Update()
        {
            //Exit game scene if cannot connect after a while
            if (game_data == null || game_data.state == GameState.Connecting || game_data.state == GameState.Starting)
            {
                timer += Time.deltaTime;
                if (!game_settings.IsHost() && timer > 10f)
                {
                    SceneNav.GoTo("Menu");
                }
            }
        }

        //--------------------

        public virtual void ConnectToAPI()
        {
            //Should already be connected to API from the menu
            //If not connected, start in test mode (this means game scene was launched directly from Unity)
            if (!Authenticator.Get().IsSignedIn())
            {
                Authenticator.Get().LoginTest("Player");

                player_settings.deck = GameplayData.Get().test_deck.id;
                ai_settings.deck = GameplayData.Get().test_deck_ai.id;
                ai_settings.ai_level = GameplayData.Get().ai_level;
            }
            
            //Set avatar, cardback based on your api data
            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                player_settings.avatar = udata.GetAvatar();
                player_settings.cardback = udata.GetCardback();
            }
        }

        public virtual async void ConnectToServer()
        {
            await Task.Yield(); //Wait for initialization to finish

            if (TcgNetwork.Get().IsActive())
                return; // Already connected

            if (game_settings.IsHost())
                TcgNetwork.Get().StartHost(NetworkData.Get().port);
            else
                TcgNetwork.Get().StartClient(game_settings.GetUrl(), NetworkData.Get().port);
        }

        public virtual async void ConnectToGame(string uid)
        {
            await Task.Yield(); //Wait for initialization to finish

            if (!TcgNetwork.Get().IsActive())
                return; //Not connected to server

            MsgPlayerConnect nplayer = new MsgPlayerConnect();
            nplayer.user_id = Authenticator.Get().UserID;
            nplayer.username = Authenticator.Get().Username;
            nplayer.game_uid = uid;
            nplayer.nb_players = game_settings.nb_players;
            nplayer.observer = game_settings.play_mode == PlayMode.Observer;

            Messaging.SendObject("connect", ServerID, nplayer, NetworkDelivery.Reliable);
        }

        public virtual void SendGameSettings()
        {
            PlayMode pmode = game_settings.play_mode;

            if (pmode == PlayMode.Solo)
            {
                //Solo mode, send both your settings and AI settings
                SendGameplaySettings(game_settings);
                SendPlayerSettingsAI(ai_settings);
                SendPlayerSettings(player_settings);
            }
            else
            {
                //Online mode, only send your own settings
                SendGameplaySettings(game_settings);
                SendPlayerSettings(player_settings);
            }
        }


        public virtual void Disconnect()
        {
            TcgNetwork.Get().Disconnect();
        }

        private void RegisterRefresh(ushort tag, UnityAction<SerializedData> callback)
        {
            RefreshEvent cmdevt = new RefreshEvent();
            cmdevt.tag = tag;
            cmdevt.callback = callback;
            registered_commands.Add(tag, cmdevt);
        }

        public void OnReceiveRefresh(ulong client_id, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ushort type);
            bool found = registered_commands.TryGetValue(type, out RefreshEvent command);
            if (found)
            {
                command.callback.Invoke(new SerializedData(reader));
            }
        }

        //--------------------------

        public void SendPlayerSettings(PlayerSettings psettings)
        {
            SendAction(GameAction.PlayerSettings, psettings);
        }

        public void SendPlayerSettingsAI(PlayerSettings psettings)
        {
            SendAction(GameAction.PlayerSettingsAI, psettings);
        }

        public void SendGameplaySettings(GameSettings settings)
        {
            SendAction(GameAction.GameplaySettings, settings);
        }

        public void PlayCard(Card card, Slot slot)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.card_uid = card.uid;
            mdata.slot = slot;
            SendAction(GameAction.PlayCard, mdata);
        }

        public void AttackTarget(Card card, Card target)
        {
            MsgAttack mdata = new MsgAttack();
            mdata.attacker_uid = card.uid;
            mdata.target_uid = target.uid;
            SendAction(GameAction.Attack, mdata);
        }

        public void AttackPlayer(Card card, Player target)
        {
            MsgAttackPlayer mdata = new MsgAttackPlayer();
            mdata.attacker_uid = card.uid;
            mdata.target_id = target.player_id;
            SendAction(GameAction.AttackPlayer, mdata);
        }

        public void Move(Card card, Slot slot)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.card_uid = card.uid;
            mdata.slot = slot;
            SendAction(GameAction.Move, mdata);
        }

        public void CastAbility(Card card, AbilityData ability)
        {
            MsgCastAbility mdata = new MsgCastAbility();
            mdata.caster_uid = card.uid;
            mdata.ability_id = ability.id;
            mdata.target_uid = "";
            SendAction(GameAction.CastAbility, mdata);
        }

        public void SelectCard(Card card)
        {
            MsgCard mdata = new MsgCard();
            mdata.card_uid = card.uid;
            SendAction(GameAction.SelectCard, mdata);
        }

        public void SelectPlayer(Player player)
        {
            MsgPlayer mdata = new MsgPlayer();
            mdata.player_id = player.player_id;
            SendAction(GameAction.SelectPlayer, mdata);
        }

        public void SelectSlot(Slot slot)
        {
            SendAction(GameAction.SelectSlot, slot);
        }

        public void SelectChoice(int c)
        {
            MsgChoice choice = new MsgChoice();
            choice.choice = c;
            SendAction(GameAction.SelectChoice, choice);
        }

        public void CancelSelection()
        {
            SendAction(GameAction.CancelSelect);
        }

        public void SendChatMsg(string msg)
        {
            MsgChat chat = new MsgChat();
            chat.msg = msg;
            chat.player_id = player_id;
            SendAction(GameAction.ChatMessage, chat);
        }

        public void EndTurn()
        {
            SendAction(GameAction.EndTurn);
        }

        public void Resign()
        {
            SendAction(GameAction.Resign);
        }

        public void SetObserverMode(int player_id)
        {
            observe_mode = true;
            observe_player_id = player_id;
        }

        public void SetObserverMode(string username)
        {
            observe_player_id = 0; //Default value of observe_user not found

            Game data = GetGameData();
            foreach (Player player in data.players)
            {
                if (player.username == username)
                {
                    observe_player_id = player.player_id;
                }
            }
        }

        public void SendAction<T>(ushort type, T data) where T : INetworkSerializable
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);
            writer.WriteNetworkSerializable(data);
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable);
            writer.Dispose();
        }

        public void SendAction(ushort type, int data)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);
            writer.WriteValueSafe(data);
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable);
            writer.Dispose();
        }

        public void SendAction(ushort type)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable);
            writer.Dispose();
        }

        //-------------------------

        protected virtual void OnConnectServer()
        {
            ConnectToGame(game_settings.game_uid);
            onConnectServer?.Invoke();
        }

        protected virtual void OnConnectedToGame(SerializedData sdata)
        {
            MsgAfterConnected msg = sdata.Get<MsgAfterConnected>();
            player_id = msg.player_id;
            game_data = msg.game_data;
            observe_mode = player_id < 0; //Will usually return -1 if its an observer

            if (observe_mode)
                SetObserverMode(observe_user);

            SendGameSettings();

            if (onConnectGame != null)
                onConnectGame.Invoke();
        }

        private void OnGameStart( SerializedData sdata)
        {
            onGameStart?.Invoke();
        }

        private void OnGameEnd(SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            onGameEnd?.Invoke(msg.player_id);
        }

        private void OnNewTurn(SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            onNewTurn?.Invoke(msg.player_id);
        }

        private void OnCardPlayed(SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardPlayed?.Invoke(card, msg.slot);
        }

        private void OnCardSummoned(SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            onCardSummoned?.Invoke(msg.slot);
        }

        private void OnCardMoved(SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardMoved?.Invoke(card, msg.slot);
        }

        private void OnCardTransformed(SerializedData sdata)
        {
            MsgCard msg = sdata.Get<MsgCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardTransformed?.Invoke(card);
        }

        private void OnCardDiscarded(SerializedData sdata)
        {
            MsgCard msg = sdata.Get<MsgCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardDiscarded?.Invoke(card);
        }

        private void OnAttackStart(SerializedData sdata)
        {
            MsgAttack msg = sdata.Get<MsgAttack>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Card target = game_data.GetCard(msg.target_uid);
            onAttackStart?.Invoke(attacker, target);
        }

        private void OnAttackEnd(SerializedData sdata)
        {
            MsgAttack msg = sdata.Get<MsgAttack>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Card target = game_data.GetCard(msg.target_uid);
            onAttackEnd?.Invoke(attacker, target);
        }

        private void OnAttackPlayerStart(SerializedData sdata)
        {
            MsgAttackPlayer msg = sdata.Get<MsgAttackPlayer>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Player target = game_data.GetPlayer(msg.target_id);
            onAttackPlayerStart?.Invoke(attacker, target);
        }

        private void OnAttackPlayerEnd(SerializedData sdata)
        {
            MsgAttackPlayer msg = sdata.Get<MsgAttackPlayer>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Player target = game_data.GetPlayer(msg.target_id);
            onAttackPlayerEnd?.Invoke(attacker, target);
        }

        private void OnAbilityTrigger(SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            onAbilityStart?.Invoke(ability, caster);
        }

        private void OnAbilityTargetCard(SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            Card target = game_data.GetCard(msg.target_uid);
            onAbilityTargetCard?.Invoke(ability, caster, target);
        }

        private void OnAbilityTargetPlayer(SerializedData sdata)
        {
            MsgCastAbilityPlayer msg = sdata.Get<MsgCastAbilityPlayer>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            Player target = game_data.GetPlayer(msg.target_id);
            onAbilityTargetPlayer?.Invoke(ability, caster, target);
        }

        private void OnAbilityTargetSlot(SerializedData sdata)
        {
            MsgCastAbilitySlot msg = sdata.Get<MsgCastAbilitySlot>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            onAbilityTargetSlot?.Invoke(ability, caster, msg.slot);
        }

        private void OnAbilityAfter(SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            onAbilityEnd?.Invoke(ability, caster);
        }

        private void OnSecret(SerializedData sdata)
        {
            MsgSecret msg = sdata.Get<MsgSecret>();
            Card secret = game_data.GetCard(msg.secret_uid);
            Card triggerer = game_data.GetCard(msg.triggerer_uid);
            onSecret?.Invoke(secret, triggerer);
        }

        private void OnChat(SerializedData sdata)
        {
            MsgChat msg = sdata.Get<MsgChat>();
            onChatMsg?.Invoke(msg.player_id, msg.msg);
        }

        private void OnServerMsg(SerializedData sdata)
        {
            string msg = sdata.GetString();
            onServerMsg?.Invoke(msg);
        }

        private void OnRefreshAll(SerializedData sdata)
        {
            MsgRefreshAll msg = sdata.Get<MsgRefreshAll>();
            game_data = msg.game_data;
            onRefreshAll?.Invoke();
        }

        //--------------------------

        public virtual bool IsReady()
        {
            return game_data != null && TcgNetwork.Get().IsConnected();
        }

        public Player GetPlayer()
        {
            Game gdata = GetGameData();
            return gdata.GetPlayer(GetPlayerID());
        }

        public Player GetOpponentPlayer()
        {
            Game gdata = GetGameData();
            return gdata.GetPlayer(GetOpponentPlayerID());
        }

        public int GetPlayerID()
        {
            if (observe_mode)
                return observe_player_id;
            return player_id;
        }

        public int GetOpponentPlayerID()
        {
            return GetPlayerID() == 0 ? 1 : 0;
        }

        public virtual bool IsYourTurn()
        {
            int player_id = GetPlayerID();
            Game game_data = GetGameData();

            if (!IsReady())
                return false;
            return player_id == game_data.current_player;
        }

        public bool IsObserveMode()
        {
            return observe_mode;
        }

        public Game GetGameData()
        {
            return game_data;
        }

        public bool HasEnded()
        {
            return game_data.HasEnded();
        }

        private void OnApplicationQuit()
        {
            Resign(); //Auto Resign before closing the app. NOTE: doesn't seem to work since the msg dont have time to be sent before it closes
        }

        public bool IsHost { get { return TcgNetwork.Get().IsHost; } }
        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } }

        public static GameClient Get()
        {
            return _instance;
        }

    }

    public class RefreshEvent
    {
        public ushort tag;
        public UnityAction<SerializedData> callback;
    }
}