using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    //Contains all gameplay state data that is sync across network

    [System.Serializable]
    public class Game
    {
        public int nb_players = 2;
        public GameSettings settings;
        public string game_uid;

        //Game state
        public int first_player = 0;
        public int current_player = -1;
        public int turn_count = 0;
        public float turn_timer = 0f;

        public GameState state = GameState.Connecting;

        //Players
        public Player[] players;

        //Selector
        public SelectorType selector = SelectorType.None;
        public int selector_player = 0;
        public string selector_ability_id;
        public string selector_caster_uid;

        //Other values (not serialized)
        [System.NonSerialized] public Card last_played;
        [System.NonSerialized] public Card last_target;
        [System.NonSerialized] public Card last_killed;
        [System.NonSerialized] public Card ability_triggerer;

        //Other arrays  (not serialized)
        [System.NonSerialized] public HashSet<string> ability_played = new HashSet<string>();
        [System.NonSerialized] public HashSet<string> cards_attacked = new HashSet<string>();

        public Game() { }
        
        public Game(string uid, int nb_players)
        {
            this.game_uid = uid;
            this.nb_players = nb_players;
            players = new Player[nb_players];
            for (int i = 0; i < nb_players; i++)
                players[i] = new Player(i);
            settings = GameSettings.Default;
        }

        public virtual bool AreAllPlayersReady()
        {
            int ready = 0;
            foreach (Player player in players)
            {
                if (player.IsReady())
                    ready++;
            }
            return ready >= nb_players;
        }

        public virtual bool AreAllPlayersConnected()
        {
            int ready = 0;
            foreach (Player player in players)
            {
                if (player.IsConnected())
                    ready++;
            }
            return ready >= nb_players;
        }

        //Check if its player's turn
        public virtual bool IsPlayerTurn(Player player)
        {
            return IsPlayerActionTurn(player) || IsPlayerSelectorTurn(player);
        }

        public virtual bool IsPlayerActionTurn(Player player)
        {
            return player != null && current_player == player.player_id 
                && state == GameState.Play && selector == SelectorType.None;
        }

        public virtual bool IsPlayerSelectorTurn(Player player)
        {
            return player != null && selector_player == player.player_id 
                && state == GameState.Play && selector != SelectorType.None;
        }
        
        //Check if a card is allowed to be played on slot
        public virtual bool CanPlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (card == null)
                return false;

            Player player = GetPlayer(card.player_id);
            if (!skip_cost && !player.CanPayMana(card))
                return false; //Cant pay mana
            if (!player.HasCard(player.cards_hand, card))
                return false; // Card not in hand

            if (card.CardData.IsBoardCard())
            {
                if (!slot.IsValid() || IsCardOnSlot(slot))
                    return false;   //Slot already occupied
                if (card.player_id != slot.p)
                    return false; //Cant play on opponent side
                return true;
            }
            if (card.CardData.IsRequireTarget())
            {
                return IsPlayTargetValid(card, slot); //Check play target on slot
            }
            return true;
        }

        //Check if a card is allowed to move to slot
        public virtual bool CanMoveCard(Card card, Slot slot)
        {
            if (card == null || !slot.IsValid())
                return false;

            if (!card.CanMove())
                return false; //Card cant move

            if (card.player_id != slot.p)
                return false; //Card played wrong side

            if (card.slot == slot)
                return false; //Cant move to same slot

            return true;
        }

        //Check if a card is allowed to attack a player
        public virtual bool CanAttackTarget(Card attacker, Player target)
        {
            if(attacker == null || target == null)
                return false;

            if (!attacker.CanAttack())
                return false; //Card cant attack

            if (attacker.player_id == target.player_id)
                return false; //Cant attack same player

            if (!IsOnBoard(attacker) || !attacker.CardData.IsCharacter())
                return false; //Cards not on board

            if (target.HasStatusEffect(StatusType.Protected) && !attacker.HasStatus(StatusType.Flying))
                return false; //Protected by taunt

            return true;
        }

        //Check if a card is allowed to attack another one
        public virtual bool CanAttackTarget(Card attacker, Card target)
        {
            if (attacker == null || target == null)
                return false;

            if (!attacker.CanAttack())
                return false; //Card cant attack

            if (attacker.player_id == target.player_id)
                return false; //Cant attack same player

            if (!IsOnBoard(attacker) || !IsOnBoard(target))
                return false; //Cards not on board

            if (!attacker.CardData.IsCharacter() || !target.CardData.IsBoardCard())
                return false; //Only character can attack

            if (target.HasStatus(StatusType.Stealth))
                return false; //Stealth cant be attacked

            if (target.HasStatus(StatusType.Protected) && !attacker.HasStatus(StatusType.Flying))
                return false; //Protected by adjacent card

            return true;
        }

        public virtual bool CanCastAbility(Card card, AbilityData ability)
        {
            if (card == null || !card.CanDoActivatedAbilities())
                return false; //This card cant cast

            if (ability.trigger != AbilityTrigger.Activate)
                return false; //Not an activated ability

            Player player = GetPlayer(card.player_id);
            if (!player.CanPayAbility(card, ability))
                return false; //Cant pay for ability

            if (!ability.AreTriggerConditionsMet(this, card))
                return false; //Conditions not met

            return true;
        }

        //Check if Player play target is valid, play target is the target when a spell requires to drag directly onto another card
        public virtual bool IsPlayTargetValid(Card caster, Player target, bool ai_check = false)
        {
            if (caster == null || target == null)
                return false;

            foreach (AbilityData ability in caster.CardData.abilities)
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget)
                {
                    if (!ability.CanTarget(this, caster, target, ai_check))
                        return false;
                }
            }
            return true;
        }

        //Check if Card play target is valid, play target is the target when a spell requires to drag directly onto another card
        public virtual bool IsPlayTargetValid(Card caster, Card target, bool ai_check = false)
        {
            if (caster == null || target == null)
                return false;

            foreach (AbilityData ability in caster.CardData.abilities)
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget)
                {
                    if (!ability.CanTarget(this, caster, target, ai_check))
                        return false;
                }
            }
            return true;
        }

        //Check if Slot play target is valid, play target is the target when a spell requires to drag directly onto another card
        public virtual bool IsPlayTargetValid(Card caster, Slot target, bool ai_check = false)
        {
            if (caster == null || target == null)
                return false;

            if (!target.IsValid())
                return IsPlayTargetValid(caster, GetPlayer(target.p)); //Slot 0,0, means we are targeting a player

            Card slot_card = GetSlotCard(target);
            if (slot_card != null)
                return IsPlayTargetValid(caster, slot_card, ai_check); //Slot has card, check play target on that card

            foreach (AbilityData ability in caster.CardData.abilities)
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget)
                {
                    if (!ability.CanTarget(this, caster, target, ai_check))
                        return false;
                }
            }
            return true;
        }

        public Player GetPlayer(int id)
        {
            if (id >= 0 && id < players.Length)
                return players[id];
            return null;
        }

        public Player GetActivePlayer()
        {
            return GetPlayer(current_player);
        }

        public Player GetOpponentPlayer(int id)
        {
            int oid = id == 0 ? 1 : 0;
            return GetPlayer(oid);
        }

        public Card GetCard(string card_uid)
        {
            foreach (Player player in players)
            {
                Card acard = player.GetCard(card_uid);
                if (acard != null)
                    return acard;
            }
            return null;
        }

        public Card GetBoardCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_board)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetHandCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_hand)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetDeckCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_deck)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetDiscardCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_discard)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetSecretCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_secret)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetSlotCard(Slot slot)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_board)
                {
                    if (card != null && card.slot == slot)
                        return card;
                }
            }
            return null;
        }
        
        public virtual Player GetRandomPlayer()
        {
            Player player = GetPlayer(Random.value > 0.5f ? 1 : 0);
            return player;
        }

        public virtual Card GetRandomBoardCard()
        {
            Player player = GetRandomPlayer();
            return player.GetRandomCard(player.cards_board);
        }

        public virtual Slot GetRandomSlot()
        {
            Player player = GetRandomPlayer();
            return player.GetRandomSlot();
        }

        public bool IsInHand(Card card)
        {
            return card != null && GetHandCard(card.uid) != null;
        }

        public bool IsOnBoard(Card card)
        {
            return card != null && GetBoardCard(card.uid) != null;
        }

        public bool IsInDeck(Card card)
        {
            return card != null && GetDeckCard(card.uid) != null;
        }

        public bool IsInDiscard(Card card)
        {
            return card != null && GetDiscardCard(card.uid) != null;
        }

        public bool IsInSecret(Card card)
        {
            return card != null && GetSecretCard(card.uid) != null;
        }

        public bool IsCardOnSlot(Slot slot)
        {
            return GetSlotCard(slot) != null;
        }

        public bool HasStarted()
        {
            return state != GameState.Connecting;
        }

        public bool HasEnded()
        {
            return state == GameState.GameEnded;
        }

        public static Game CloneNew(Game source)
        {
            Game game = new Game();
            Clone(source, game);
            return game;
        }

        public static void Clone(Game source, Game dest)
        {
            dest.game_uid = source.game_uid;
            dest.nb_players = source.nb_players;
            dest.settings = source.settings;

            dest.first_player = source.first_player;
            dest.current_player = source.current_player;
            dest.turn_count = source.turn_count;
            dest.turn_timer = source.turn_timer;
            dest.state = source.state;

            if (dest.players == null)
            {
                dest.players = new Player[source.players.Length];
                for(int i=0; i< source.players.Length; i++)
                    dest.players[i] = new Player(i);
            }

            for (int i = 0; i < source.players.Length; i++)
                Player.Clone(source.players[i], dest.players[i]);

            dest.selector = source.selector;
            dest.selector_player = source.selector_player;
            dest.selector_caster_uid = source.selector_caster_uid;
            dest.selector_ability_id = source.selector_ability_id;

            //No need to copy temporary data for optimization
            //dest.last_played = source.last_played;
            //dest.last_killed = source.last_killed;
            //dest.last_target = source.last_target;

            //CloneHash(source.ability_played, dest.ability_played);
            //CloneHash(source.cards_attacked, dest.cards_attacked);
        }

        public static void CloneHash(HashSet<string> source, HashSet<string> dest)
        {
            dest.Clear();
            foreach (string str in source)
                dest.Add(str);
        }
    }

    [System.Serializable]
    public enum GameState
    {
        Connecting = 0, //Players are not connected
        Starting = 1,  //Players are ready and connected, game is setting-up

        StartTurn = 10, //Start of turn effects
        Play = 20,      //Play step
        EndTurn = 30,   //End of turn effects

        GameEnded = 99,
    }

    [System.Serializable]
    public enum SelectorType
    {
        None = 0,
        SelectTarget = 10,
        SelectorCard = 20,
        SelectorChoice = 30,
    }
}