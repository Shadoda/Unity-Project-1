using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// Execute and resolves game rules and logic
    /// </summary>

    public class GameLogic
    {
        public UnityAction onGameStart;
        public UnityAction<Player> onGameEnd;          //Winner

        public UnityAction onTurnStart;
        public UnityAction onTurnPlay;
        public UnityAction onTurnEnd;

        public UnityAction<Card, Slot> onCardPlayed;      
        public UnityAction<Card, Slot> onCardSummoned;
        public UnityAction<Card, Slot> onCardMoved;
        public UnityAction<Card> onCardTransformed;
        public UnityAction<Card> onCardDiscarded;

        public UnityAction<AbilityData, Card> onAbilityStart;        
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;  //Ability, Caster, Target
        public UnityAction<AbilityData, Card, Player> onAbilityTargetPlayer;
        public UnityAction<AbilityData, Card, Slot> onAbilityTargetSlot;
        public UnityAction<AbilityData, Card> onAbilityEnd;

        public UnityAction<Card, Card> onAttackStart;  //Attacker, Defender
        public UnityAction<Card, Card> onAttackEnd;     //Attacker, Defender
        public UnityAction<Card, Player> onAttackPlayerStart;
        public UnityAction<Card, Player> onAttackPlayerEnd;

        public UnityAction<Card, Card> onSecret;    //Secret, Triggerer

        public UnityAction onSelectorStart;
        public UnityAction onSelectorSelect;

        private Game game_data;

        private System.Random random_gen = new System.Random();

        private ListSwap<Card> card_array = new ListSwap<Card>();
        private ListSwap<Player> player_array = new ListSwap<Player>();
        private ListSwap<Slot> slot_array = new ListSwap<Slot>();

        private Pool<AbilityQueueElement> ability_elem_pool = new Pool<AbilityQueueElement>();
        private Pool<SecretQueueElement> secret_elem_pool = new Pool<SecretQueueElement>();
        private Pool<AttackQueueElement> attack_elem_pool = new Pool<AttackQueueElement>();
        private Pool<CallbackQueueElement> callback_elem_pool = new Pool<CallbackQueueElement>();
        
        private Queue<AbilityQueueElement> ability_cast_queue = new Queue<AbilityQueueElement>();
        private Queue<SecretQueueElement> secret_queue = new Queue<SecretQueueElement>();
        private Queue<AttackQueueElement> attack_queue = new Queue<AttackQueueElement>();
        private Queue<CallbackQueueElement> callback_queue = new Queue<CallbackQueueElement>();
        private bool is_resolving = false;

        public GameLogic(){ }

        public GameLogic(Game game)
        {
            game_data = game;
        }

        public virtual void SetData(Game game)
        {
            game_data = game;
        }

        //----- Turn Phases ----------

        public virtual void StartGame()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            //Choose first player
            game_data.first_player = random_gen.NextDouble() < 0.5 ? 0 : 1;
            game_data.current_player = game_data.first_player;
            game_data.turn_count = 1;

            //Start state
            game_data.state = GameState.Starting;
            onGameStart?.Invoke();

            //Init each player
            foreach (Player player in game_data.players)
            {
                //Hp / mana
                player.hp = GameplayData.Get().hp_start;
                player.hp_max = GameplayData.Get().hp_start;
                player.mana = GameplayData.Get().mana_start;
                player.mana_max = GameplayData.Get().mana_start;

                //Shuffle and draw
                ShuffleDeck(player.cards_deck);
                DrawCard(player.player_id, GameplayData.Get().cards_start);

                //Add coin second player
                if (player.player_id != game_data.first_player && GameplayData.Get().second_bonus != null)
                {
                    Card card = Card.Create(GameplayData.Get().second_bonus.id, player.player_id);
                    player.cards_all[card.uid] = card;
                    player.cards_hand.Add(card);
                }
            }

            StartTurn();
        }
		
        public virtual void StartTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            ClearTurnData();
            game_data.state = GameState.StartTurn;
            onTurnStart?.Invoke();

            Player player = game_data.GetActivePlayer();

            //Cards draw
            if (game_data.turn_count > 1 || player.player_id != game_data.first_player)
            {
                DrawCard(player.player_id, 1);
            }

            //Actions
            player.mana_max += 1;
            player.mana_max = Mathf.Min(player.mana_max, GameplayData.Get().mana_max);
            player.mana = player.mana_max;
            player.history_list.Clear();

            //Turn timer
            game_data.turn_timer = GameplayData.Get().turn_duration;

            //Ongoing Abilities
            UpdateOngoingAbilities();

            if (player.HasStatusEffect(StatusType.Poisoned))
                player.hp -= player.GetStatusEffectValue(StatusType.Poisoned);

            //StartTurn Abilities
            for (int i = player.cards_board.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_board[i];
                TriggerCardAbilityType(AbilityTrigger.StartOfTurn, card);

                if (card.HasStatus(StatusType.Poisoned))
                    DamageCard(card, card.GetStatusValue(StatusType.Poisoned));
            }

            AddCallbackToQueue(StartPlayPhase);
            ResolveAll();
        }

        public virtual void StartNextTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.current_player = (game_data.current_player + 1) % game_data.nb_players;
            
            if (game_data.current_player == game_data.first_player)
                game_data.turn_count++;

            CheckForWinner();
            StartTurn();
        }

        public virtual void StartPlayPhase()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.state = GameState.Play;
            onTurnPlay?.Invoke();
        }

        public virtual void EndTurn()
        {
            if (game_data.state != GameState.Play)
                return;

            game_data.selector = SelectorType.None;
            game_data.state = GameState.EndTurn;
            onTurnEnd?.Invoke();

            //Remove status effects with duration
            Player player = game_data.GetActivePlayer();
            foreach (Player aplayer in game_data.players)
            {
                foreach (Card card in aplayer.cards_board)
                {
                    for (int i = card.status.Count - 1; i >= 0; i--)
                    {
                        if (!card.status[i].permanent)
                        {
                            card.status[i].duration -= 1;
                            if (card.status[i].duration <= 0)
                                card.status.RemoveAt(i);
                        }
                    }
                }
            }

            //Refresh secrets
            foreach (Card card in player.cards_secret)
            {
                card.Refresh();
            }

            //Refresh current player cards on board
            for (int i = player.cards_board.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_board[i];
                card.Refresh();
                TriggerCardAbilityType(AbilityTrigger.EndOfTurn, card);
            }

            AddCallbackToQueue(StartNextTurn);
            ResolveAll();
        }

        //End game with winner
        public virtual void EndGame(int winner)
        {
            if (game_data.state != GameState.GameEnded)
            {
                game_data.state = GameState.GameEnded;
                game_data.current_player = winner; //Winner player
                Player player = game_data.GetPlayer(winner);
                onGameEnd?.Invoke(player);
            }
        }

        //Progress to the next step/phase 
        public virtual void NextStep()
        {
            if (game_data.selector != SelectorType.None)
            {
                CancelSelection();
            }
            else if (game_data.state == GameState.Play)
            {
                EndTurn();
            }
        }

        //Check if a player is winning the game, if so end the game
        //Change or edit this function for a new win condition
        protected virtual void CheckForWinner()
        {
            int count_alive = 0;
            Player alive = null;
            foreach (Player player in game_data.players)
            {
                if (!player.IsDead())
                {
                    alive = player;
                    count_alive++;
                }
            }

            if (count_alive == 0)
            {
                EndGame(-1); //Everyone is dead, Draw
            }
            else if (count_alive == 1)
            {
                EndGame(alive.player_id); //Player win
            }
        }

        protected virtual void ClearTurnData()
        {
            game_data.selector = SelectorType.None;
            attack_elem_pool.DisposeAll();
            ability_elem_pool.DisposeAll();
            secret_elem_pool.DisposeAll();
            callback_elem_pool.DisposeAll();
            attack_queue.Clear();
            ability_cast_queue.Clear();
            secret_queue.Clear();
            callback_queue.Clear();
            card_array.Clear();
            player_array.Clear();
            slot_array.Clear();
            game_data.last_played = null;
            game_data.last_killed = null;
            game_data.last_target = null;
            game_data.ability_triggerer = null;
            game_data.ability_played.Clear();
            game_data.cards_attacked.Clear();
        }

        //--- Setup ------

        //Set deck using a Deck in Resources
        public virtual void SetPlayerDeck(int player_id, string deck_id, CardData[] cards)
        {
            Player player = game_data.GetPlayer(player_id);
            player.cards_all.Clear();
            player.cards_deck.Clear();
            player.deck = deck_id;

            foreach (CardData card in cards)
            {
                Card acard = Card.Create(card.id, player.player_id);
                player.cards_all[acard.uid] = acard;
                player.cards_deck.Add(acard);
            }

            //Shuffle deck
            ShuffleDeck(player.cards_deck);
        }

        //Set deck using custom deck in save file or database
        public virtual void SetPlayerDeck(int player_id, UserDeckData deck)
        {
            Player player = game_data.GetPlayer(player_id);
            
            player.cards_all.Clear();
            player.cards_deck.Clear();
            player.deck = deck.tid;

            foreach (string tid in deck.cards)
            {
                string card_id = UserCardData.GetCardId(tid);
                CardVariant variant = UserCardData.GetCardVariant(tid);

                Card acard = Card.Create(card_id, player.player_id);
                acard.variant = variant;
                player.cards_all[acard.uid] = acard;
                player.cards_deck.Add(acard);
            }

            //Shuffle deck
            ShuffleDeck(player.cards_deck);
        }

        //---- Gameplay Actions --------------

        public virtual void PlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            Player player = game_data.GetPlayer(card.player_id);
            if (player == null || card == null)
                return; //Cant find data

            if (game_data.CanPlayCard(card, slot, skip_cost))
            {
                //Cost
                if (!skip_cost)
                    player.PayMana(card);

                //Play card
                player.RemoveCardFromAllGroups(card);
                card.Cleanse();

                //Add to board
                CardData icard = card.CardData;
                if (icard.IsBoardCard())
                {
                    player.cards_board.Add(card);
                    card.slot = slot;
                    card.SetCard(icard);      //Reset all stats to default
                    card.exhausted = true; //Cant attack first turn
                }
                else if (icard.IsSecret())
                {
                    player.cards_secret.Add(card);
                    card.exhausted = true;
                }
                else
                {
                    player.cards_discard.Add(card);
                    card.slot = slot; //Save slot in case spell has PlayTarget
                }

                //History
                if(!icard.IsSecret())
                    player.AddHistory(GameAction.PlayCard, card);

                //Update ongoing effects
                game_data.last_played = card;
                UpdateOngoingAbilities();

                //Trigger abilities
                TriggerSecrets(AbilityTrigger.OnPlayOther, card); //After playing card
                TriggerCardAbilityType(AbilityTrigger.OnPlay, card);

                foreach (Player oplayer in game_data.players)
                {
                    foreach (Card ocard in oplayer.cards_board)
                        TriggerCardAbilityType(AbilityTrigger.OnPlayOther, ocard, card);
                }

                onCardPlayed?.Invoke(card, slot);
                ResolveAll();
            }
        }

        public virtual void MoveCard(Card card, Slot slot)
        {
            Player player = game_data.GetPlayer(card.player_id);
            if (player == null || card == null)
                return;
            
            Card slot_card = game_data.GetSlotCard(slot);
            if (slot_card != null || !slot.IsValid())
                return; //Cant move to already occipied slot

            if (game_data.CanMoveCard(card, slot))
            {
                card.slot = slot;

                //Moving doesn't really have any effect in demo so can be done indefinitely
                //card.exhausted = true;
                //card.RemoveStatus(StatusEffect.Stealth);
                //player.AddHistory(GameAction.Move, card);

                UpdateOngoingAbilities();

                onCardMoved?.Invoke(card, slot);
                ResolveAll();
            }
        }

        public virtual void CastAbility(Card card, AbilityData iability)
        {
            Player player = game_data.GetPlayer(card.player_id);
            if (player == null || card == null || iability == null)
                return;

            CardData icard = card.CardData;
            if (icard != null)
            {
                if (iability != null && game_data.CanCastAbility(card, iability))
                {
                    if (!iability.IsSelectTarget())
                        player.AddHistory(GameAction.CastAbility, card, iability);
                    card.RemoveStatus(StatusType.Stealth);
                    TriggerCardAbility(iability, card);
                    ResolveAll();
                }
            }
        }

        public virtual void AttackTarget(Card attacker, Card target)
        {
            if (attacker == null || target == null)
                return;

            if (!game_data.CanAttackTarget(attacker, target))
                return;

            Player player = game_data.GetPlayer(attacker.player_id);
            player.AddHistory(GameAction.Attack, attacker, target);

            //Trigger before attack abilities
            TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);
            TriggerCardAbilityType(AbilityTrigger.OnBeforeDefend, target, attacker);
            TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
            TriggerSecrets(AbilityTrigger.OnBeforeDefend, target);

            //Resolve attack
            AddAttackToQueue(attacker, target);

            ResolveAll();
        }

        protected virtual void ResolveAttack(Card attacker, Card target)
        {
            onAttackStart?.Invoke(attacker, target);

            attacker.RemoveStatus(StatusType.Stealth);
            UpdateOngoingAbilities();

            //Count attack damage
            int datt1 = attacker.GetAttack();
            int datt2 = target.GetAttack();

            //Damage Cards
            DamageCard(attacker, target, datt1);
            DamageCard(target, attacker, datt2);

            //Save attack and exhaust
            ExhaustBattle(attacker);

            //Recalculate bonus
            UpdateOngoingAbilities();

            //Abiltiies
            bool att_board = game_data.IsOnBoard(attacker);
            bool def_board = game_data.IsOnBoard(target);
            if (att_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);
            if (def_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterDefend, target, attacker);
            if (att_board)
                TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);
            if (def_board)
                TriggerSecrets(AbilityTrigger.OnAfterDefend, target);

            onAttackEnd?.Invoke(attacker, target);
        }

        public virtual void AttackPlayer(Card attacker, Player target)
        {
            if (attacker == null || target == null)
                return;

            if (!game_data.CanAttackTarget(attacker, target))
                return;

            Player player = game_data.GetPlayer(attacker.player_id);
            player.AddHistory(GameAction.AttackPlayer, attacker, target);

            //Resolve abilities
            TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
            TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);

            //Resolve attack
            AddAttackToQueue(attacker, target);

            ResolveAll();
            CheckForWinner();
        }

        protected virtual void ResolveAttackPlayer(Card attacker, Player target)
        {
            onAttackPlayerStart?.Invoke(attacker, target);

            attacker.RemoveStatus(StatusType.Stealth);
            UpdateOngoingAbilities();

            //Damage player
            int datt1 = attacker.GetAttack();
            target.hp -= datt1;
            target.hp = Mathf.Clamp(target.hp, 0, target.hp_max);

            //Save attack and exhaust
            ExhaustBattle(attacker);

            //Recalculate bonus
            UpdateOngoingAbilities();

            if (game_data.IsOnBoard(attacker))
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);

            TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);
            onAttackPlayerEnd?.Invoke(attacker, target);
        }

        //Exhaust after battle
        public virtual void ExhaustBattle(Card attacker)
        {
            bool attacked_before = game_data.cards_attacked.Contains(attacker.uid);
            game_data.cards_attacked.Add(attacker.uid);
            bool attack_again = attacker.HasStatus(StatusType.Fury) && !attacked_before;
            attacker.exhausted = !attack_again;
        }

        public virtual void ShuffleDeck(List<Card> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                Card temp = cards[i];
                int randomIndex = random_gen.Next(i, cards.Count);
                cards[i] = cards[randomIndex];
                cards[randomIndex] = temp;
            }
        }

        public virtual void DrawCard(int player_id, int nb = 1)
        {
            Player player = game_data.GetPlayer(player_id);
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count > 0 && player.cards_hand.Count < GameplayData.Get().cards_max)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_hand.Add(card);
                }
            }
        }

        //Put a card from deck into discard
        public virtual void DrawDiscardCard(int player_id, int nb = 1)
        {
            Player player = game_data.GetPlayer(player_id);
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count > 0)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_discard.Add(card);
                }
            }
        }

        //Summon copy of an exiting card
        public virtual Card SummonCopy(int player_id, Card copy, Slot slot)
        {
            if (copy == null)
                return null;

            CardData icard = copy.CardData;
            return SummonCard(player_id, icard, slot);
        }

        //Create a new card and send it to the board
        public virtual Card SummonCard(int player_id, CardData card, Slot slot)
        {
            if (!slot.IsValid())
                return null;

            if (game_data.GetSlotCard(slot) != null)
                return null;

            Card acard = SummonCardHand(player_id, card);
            PlayCard(acard, slot, true);

            onCardSummoned?.Invoke(acard, slot);

            return acard;
        }

        //Create a new card and send it to your hand
        public virtual Card SummonCardHand(int player_id, CardData card)
        {
            string uid = "s_" + GameTool.GenerateRandomID(random_gen);
            Player player = game_data.GetPlayer(player_id);
            Card acard = Card.Create(card.id, player.player_id, uid);
            player.cards_all[acard.uid] = acard;
            player.cards_hand.Add(acard);
            return acard;
        }

        //Transform card into another one
        public virtual Card TransformCard(Card card, CardData transform_to)
        {
            card.SetCard(transform_to);

            onCardTransformed?.Invoke(card);

            return card;
        }

        //Change owner of a card
        public virtual void ChangeOwner(Card card, Player owner)
        {
            Player powner = game_data.GetPlayer(card.player_id);
            powner.RemoveCardFromAllGroups(card);
            powner.cards_all.Remove(card.uid);
            owner.cards_all[card.uid] = card;
            card.player_id = owner.player_id;
        }

        //Heal a card
        public virtual void HealCard(Card target, int value)
        {
            if (target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return;

            target.damage -= value;
            target.damage = Mathf.Max(target.damage, 0);
        }

        //Generic damage that doesnt come from another card
        public virtual void DamageCard(Card target, int value)
        {
            if(target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; //Invincible

            if (target.HasStatus(StatusType.SpellImmunity))
                return; //Spell immunity

            target.damage += value;

            if (target.GetHP() <= 0)
                DiscardCard(target);
        }

        //Damage a card with attacker/caster
        public virtual void DamageCard(Card attacker, Card target, int value)
        {
            if (attacker == null || target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; //Invincible

            if (target.HasStatus(StatusType.SpellImmunity) && attacker.CardData.type != CardType.Character)
                return; //Spell immunity

            //Shell
            bool doublelife = target.HasStatus(StatusType.Shell);
            if (doublelife)
            {
                target.RemoveStatus(StatusType.Shell);
                return;
            }

            //Armor
            if (target.HasStatus(StatusType.Armor))
                value = Mathf.Max(value - target.GetStatusValue(StatusType.Armor), 0);

            int extra = value - target.GetHP();
            target.damage += value;

            //Trample
            Player tplayer = game_data.GetPlayer(target.player_id);
            if (extra > 0 && attacker.HasStatus(StatusType.Trample))
                tplayer.hp -= extra;

            //Deathtouch
            if (value > 0 && attacker.HasStatus(StatusType.Deathtouch) && target.CardData.type == CardType.Character)
                KillCard(attacker, target);

            //Kill on 0 hp
            if (target.GetHP() <= 0)
                KillCard(attacker, target);
        }

        public virtual void KillCard(Card attacker, Card target)
        {
            if (attacker == null || target == null)
                return;

            if (!game_data.IsOnBoard(target))
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return;

            Player pattacker = game_data.GetPlayer(attacker.player_id);
            if (attacker.player_id != target.player_id)
                pattacker.kill_count++;

            game_data.last_killed = target;
            DiscardCard(target);

            TriggerCardAbilityType(AbilityTrigger.OnKill, attacker, target);
        }

        //Send card into discard
        public virtual void DiscardCard(Card card)
        {
            if (card == null)
                return;

            if (game_data.IsInDiscard(card))
                return; //Already discarded

            CardData icard = card.CardData;
            Player player = game_data.GetPlayer(card.player_id);
            bool was_on_board = game_data.IsOnBoard(card);

            //Remove card from board and add to discard
            player.RemoveCardFromAllGroups(card);
            player.cards_discard.Add(card);

            if (was_on_board)
            {
                //Trigger on death abilities
                TriggerCardAbilityType(AbilityTrigger.OnDeath, card);

                foreach (Player oplayer in game_data.players)
                {
                    foreach (Card ocard in oplayer.cards_board)
                        TriggerCardAbilityType(AbilityTrigger.OnDeathOther, ocard, card);
                }
            }

            card.Cleanse();
            onCardDiscarded?.Invoke(card);
        }

        //--- Abilities --

        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Card triggerer = null)
        {
            foreach (AbilityData iability in caster.CardData.abilities)
            {
                if (iability && iability.trigger == type)
                {
                    TriggerCardAbility(iability, caster, triggerer);
                }
            }
        }

        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Card triggerer = null)
        {
            Card trigger_card = triggerer != null ? triggerer : caster; //Triggerer is the caster if not set
            if (iability.AreTriggerConditionsMet(game_data, caster, trigger_card))
            {
                AddAbilityToQueue(iability, caster, triggerer);
            }
        }

        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Player triggerer)
        {
            foreach (AbilityData iability in caster.CardData.abilities)
            {
                if (iability && iability.trigger == type)
                {
                    TriggerCardAbility(iability, caster, triggerer);
                }
            }
        }

        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Player triggerer)
        {
            if (iability.AreTriggerConditionsMet(game_data, caster, triggerer))
            {
                AddAbilityToQueue(iability, caster, caster);
            }
        }

        //Resolve a card ability, may stop to ask for target
        protected virtual void ResolveCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            if (!caster.CanDoAbilities())
                return; //Silenced card cant cast

            //Debug.Log("Trigger Ability " + iability.id + " : " + caster.card_id);

            Player player = game_data.GetPlayer(caster.player_id);
            onAbilityStart?.Invoke(iability, caster);
            game_data.ability_triggerer = triggerer;

            bool is_selector = ResolveCardAbilitySelector(iability, caster);
            if (is_selector)
                return; //Wait for player to select

            ResolveCardAbilityPlayTarget(iability, caster);
            ResolveCardAbilityPlayers(iability, caster);
            ResolveCardAbilityCards(iability, caster);
            ResolveCardAbilitySlots(iability, caster);
            ResolveCardAbilityNoTarget(iability, caster);
            AfterAbilityResolved(iability, caster);
        }

        protected virtual bool ResolveCardAbilitySelector(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.SelectTarget)
            {
                //Wait for target
                GoToSelectTarget(iability, caster);
                return true;
            }
            else if (iability.target == AbilityTarget.CardSelector)
            {
                GoToSelectorCard(iability, caster);
                return true;
            }
            else if (iability.target == AbilityTarget.ChoiceSelector)
            {
                GoToSelectorChoice(iability, caster);
                return true;
            }
            return false;
        }

        protected virtual void ResolveCardAbilityPlayTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.PlayTarget)
            {
                Slot slot = caster.slot;
                Card slot_card = game_data.GetSlotCard(slot);
                if (!slot.IsValid())
                    ResolveEffectTarget(iability, caster, game_data.GetPlayer(slot.p));
                else if (slot_card != null)
                    ResolveEffectTarget(iability, caster, slot_card);
                else
                    ResolveEffectTarget(iability, caster, slot);
            }
        }

        protected virtual void ResolveCardAbilityPlayers(AbilityData iability, Card caster)
        {
            //Get Player Targets based on conditions
            List<Player> targets = iability.GetPlayerTargets(game_data, caster, player_array.Get());

            //Filter targets
            if (iability.filters_target != null)
            {
                foreach (FilterData filter in iability.filters_target)
                    targets = filter.FilterTargets(game_data, iability, caster, targets, player_array.GetOther(targets));
            }

            //Resolve effects
            foreach (Player target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityCards(AbilityData iability, Card caster)
        {
            //Get Cards Targets based on conditions
            List<Card> targets = iability.GetCardTargets(game_data, caster, card_array.Get());

            //Filter targets
            if (iability.filters_target != null)
            {
                foreach (FilterData filter in iability.filters_target)
                    targets = filter.FilterTargets(game_data, iability, caster, targets, card_array.GetOther(targets));
            }

            //Resolve effects
            foreach (Card target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilitySlots(AbilityData iability, Card caster)
        {
            //Get Slot Targets based on conditions
            List<Slot> targets = iability.GetSlotTargets(game_data, caster, slot_array.Get());

            //Filter targets
            if (iability.filters_target != null)
            {
                foreach (FilterData filter in iability.filters_target)
                    targets = filter.FilterTargets(game_data, iability, caster, targets, slot_array.GetOther(targets));
            }

            //Resolve effects
            foreach (Slot target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityNoTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.None)
                iability.DoEffects(this, caster);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Player target)
        {
            if (target == null)
                return;

            iability.DoEffects(this, caster, target);

            onAbilityTargetPlayer?.Invoke(iability, caster, target);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Card target)
        {
            if (target == null)
                return;

            game_data.last_target = target;

            iability.DoEffects(this, caster, target);

            onAbilityTargetCard?.Invoke(iability, caster, target);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Slot target)
        {
            iability.DoEffects(this, caster, target);

            onAbilityTargetSlot?.Invoke(iability, caster, target);
        }

        protected virtual void AfterAbilityResolved(AbilityData iability, Card caster)
        {
            Player player = game_data.GetPlayer(caster.player_id);

            //Add to played
            game_data.ability_played.Add(iability.id);

            //Pay cost
            if (iability.trigger == AbilityTrigger.Activate)
            {
                player.mana -= iability.mana_cost;
                caster.exhausted = caster.exhausted || iability.exhaust;
            }

            //Recalculate and clear
            UpdateOngoingAbilities();

            //Chain ability
            if (iability.target != AbilityTarget.ChoiceSelector)
            {
                foreach (AbilityData chain_ability in iability.chain_abilities)
                {
                    TriggerCardAbility(chain_ability, caster);
                }
            }

            onAbilityEnd?.Invoke(iability, caster);
        }

        //This function is called often to update status/stats affected by ongoing abilities
        //It basically first reset the bonus to 0 (CleanOngoing) and then recalculate it to make sure it it still present
        //Only cards in hand and on board are updated in this way
        public virtual void UpdateOngoingAbilities()
        {
            Profiler.BeginSample("Update Ongoing");
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                player.CleanOngoing();

                for (int c = 0; c < player.cards_board.Count; c++)
                    player.cards_board[c].CleanOngoing();

                for (int c = 0; c < player.cards_hand.Count; c++)
                    player.cards_hand[c].CleanOngoing();
            }

            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int c = 0; c < player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];
                    if (card.CanDoAbilities())
                    {
                        //Ongoing Abilitiess
                        CardData icaster = card.CardData;
                        for (int a = 0; a < icaster.abilities.Length; a++)
                        {
                            AbilityData ability = icaster.abilities[a];
                            if (ability != null && ability.trigger == AbilityTrigger.Ongoing && ability.AreTriggerConditionsMet(game_data, card))
                            {
                                if (ability.target == AbilityTarget.Self)
                                {
                                    if (ability.AreTargetConditionsMet(game_data, card, card))
                                    {
                                        ability.DoOngoingEffects(this, card, card);
                                    }
                                }

                                if (ability.target == AbilityTarget.PlayerSelf)
                                {
                                    if (ability.AreTargetConditionsMet(game_data, card, player))
                                    {
                                        ability.DoOngoingEffects(this, card, player);
                                    }
                                }

                                if (ability.target == AbilityTarget.AllPlayers || ability.target == AbilityTarget.PlayerOpponent)
                                {
                                    for (int tp = 0; tp < game_data.players.Length; tp++)
                                    {
                                        if (ability.target == AbilityTarget.AllPlayers || tp != player.player_id)
                                        {
                                            Player oplayer = game_data.players[tp];
                                            if (ability.AreTargetConditionsMet(game_data, card, oplayer))
                                            {
                                                ability.DoOngoingEffects(this, card, oplayer);
                                            }
                                        }
                                    }
                                }

                                if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsBoard)
                                {
                                    for (int tp = 0; tp < game_data.players.Length; tp++)
                                    {
                                        Player tplayer = game_data.players[tp];
                                        if (ability.target == AbilityTarget.AllCardsAllPiles)
                                        {
                                            //Looping on all cards is very slow, since there are no ongoing effects that works out of board/hand we loop on those only
                                            for (int tc = 0; tc < tplayer.cards_hand.Count; tc++)
                                            {
                                                Card tcard = tplayer.cards_hand[tc];
                                                if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                                {
                                                    ability.DoOngoingEffects(this, card, tcard);
                                                }
                                            }
                                        }

                                        for (int tc = 0; tc < tplayer.cards_board.Count; tc++)
                                        {
                                            Card tcard = tplayer.cards_board[tc];
                                            if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                            {
                                                ability.DoOngoingEffects(this, card, tcard);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Stats bonus
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for(int c=0; c<player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];

                    //Taunt effect
                    if (card.HasStatus(StatusType.Protection))
                    {
                        player.AddOngoingStatus(StatusType.Protected, 0);

                        for (int tc = 0; tc < player.cards_board.Count; tc++)
                        {
                            Card tcard = player.cards_board[tc];
                            if (!tcard.HasStatus(StatusType.Protection) && !tcard.HasStatus(StatusType.Protected))
                            {
                                tcard.AddOngoingStatus(StatusType.Protected, 0);
                            }
                        }
                    }

                    //Status bonus
                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }
            }

            //Kill stuff with 0 hp
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int i = player.cards_board.Count - 1; i >= 0; i--)
                {
                    Card card = player.cards_board[i];
                    if (card.GetHP() <= 0)
                        DiscardCard(card);
                }
            }

            Profiler.EndSample();
        }

        protected virtual void AddOngoingStatusBonus(Card card, CardStatus status)
        {
            if (status.type == StatusType.AttackBonus)
                card.attack_ongoing_bonus += status.value;
            if (status.type == StatusType.HPBonus)
                card.hp_ongoing_bonus += status.value;
        }

        //---- Secrets ------------

        public virtual bool TriggerSecrets(AbilityTrigger secret_trigger, Card trigger_card)
        {
            if (trigger_card.HasStatus(StatusType.SpellImmunity))
                return false; //Spell Immunity, triggerer is the one that trigger the trap, target is the one attacked, so usually the player who played the trap, so we dont check the target

            bool success = false;
            for(int p=0; p < game_data.players.Length; p++ )
            {
                if (p != trigger_card.player_id)
                {
                    Player other_player = game_data.players[p];
                    for (int i = other_player.cards_secret.Count - 1; i >= 0; i--)
                    {
                        Card card = other_player.cards_secret[i];
                        CardData icard = card.CardData;
                        if (icard.type == CardType.Secret && !card.exhausted)
                        {
                            if (icard.AreSecretConditionsMet(secret_trigger, game_data, card, trigger_card))
                            {
                                AddSecretToQueue(secret_trigger, card, trigger_card);
                                card.exhausted = true;
                                success = true;
                            }
                        }
                    }
                }
            }
            return success;
        }

        protected virtual void ResolveSecret(AbilityTrigger secret_trigger, Card secret_card, Card trigger)
        {
            CardData icard = secret_card.CardData;
            Player player = game_data.GetPlayer(secret_card.player_id);
            if (icard.type == CardType.Secret)
            {
                Player tplayer = game_data.GetPlayer(trigger.player_id);
                tplayer.AddHistory(GameAction.SecretResolved, secret_card, trigger);

                TriggerCardAbilityType(secret_trigger, secret_card, trigger);
                DiscardCard(secret_card);

                if (onSecret != null)
                    onSecret.Invoke(secret_card, trigger);
            }
        }

        //---- Resolve Selector -----

        public virtual void SelectCard(Card target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; //Can't target that target

                Player player = game_data.GetPlayer(caster.player_id);
                player.AddHistory(GameAction.CastAbility, caster, ability, target);
                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                ResolveAll();
            }

            if (game_data.selector == SelectorType.SelectorCard)
            {
                if (!ability.AreTargetConditionsMet(game_data, caster, target))
                    return; //Conditions not met

                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                ResolveAll();
            }
        }

        public virtual void SelectPlayer(Player target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; //Can't target that target

                Player player = game_data.GetPlayer(caster.player_id);
                player.AddHistory(GameAction.CastAbility, caster, ability, target);
                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                ResolveAll();
            }
        }

        public virtual void SelectSlot(Slot target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || !target.IsValid())
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if(!ability.CanTarget(game_data, caster, target))
                    return; //Conditions not met

                Player player = game_data.GetPlayer(caster.player_id);
                player.AddHistory(GameAction.CastAbility, caster, ability, target);
                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                ResolveAll();
            }
        }

        public virtual void SelectChoice(int choice)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || choice < 0)
                return;

            if (game_data.selector == SelectorType.SelectorChoice && ability.target == AbilityTarget.ChoiceSelector)
            {
                if (choice >= 0 && choice < ability.chain_abilities.Length)
                {
                    AbilityData achoice = ability.chain_abilities[choice];
                    if (achoice != null && achoice.AreTriggerConditionsMet(game_data, caster))
                    {
                        game_data.selector = SelectorType.None;
                        AfterAbilityResolved(ability, caster);
                        ResolveCardAbility(achoice, caster, caster);
                        ResolveAll();
                    }
                }
            }
        }

        public virtual void CancelSelection()
        {
            if (game_data.selector != SelectorType.None)
            {
                //End selection
                game_data.selector = SelectorType.None;
                onSelectorSelect?.Invoke();
            }
        }

        //-----Trigger Selector-----

        protected virtual void GoToSelectTarget(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectTarget;
            game_data.selector_player = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            onSelectorStart?.Invoke();
        }

        protected virtual void GoToSelectorCard(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorCard;
            game_data.selector_player = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            onSelectorStart?.Invoke();
        }

        protected virtual void GoToSelectorChoice(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorChoice;
            game_data.selector_player = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            onSelectorStart?.Invoke();
        }

        //--- Add to queue --------

        protected virtual void AddAbilityToQueue(AbilityData ability, Card caster, Card triggerer)
        {
            if (ability != null && caster != null)
            {
                AbilityQueueElement elem = ability_elem_pool.Create();
                elem.caster = caster;
                elem.triggerer = triggerer;
                elem.ability = ability;
                ability_cast_queue.Enqueue(elem);
            }
        }

        protected virtual void AddAttackToQueue(Card attacker, Card target)
        {
            if (attacker != null && target != null)
            {
                AttackQueueElement elem = attack_elem_pool.Create();
                elem.attacker = attacker;
                elem.target = target;
                elem.ptarget = null;
                attack_queue.Enqueue(elem);
            }
        }

        protected virtual void AddAttackToQueue(Card attacker, Player target)
        {
            if (attacker != null && target != null)
            {
                AttackQueueElement elem = attack_elem_pool.Create();
                elem.attacker = attacker;
                elem.ptarget = target;
                elem.target = null;
                attack_queue.Enqueue(elem);
            }
        }

        protected virtual void AddCallbackToQueue(System.Action callback)
        {
            if (callback != null)
            {
                CallbackQueueElement elem = callback_elem_pool.Create();
                elem.callback = callback;
                callback_queue.Enqueue(elem);
            }
        }

        protected virtual void AddSecretToQueue(AbilityTrigger secret_trigger, Card secret, Card trigger)
        {
            if (secret != null && trigger != null)
            {
                SecretQueueElement elem = secret_elem_pool.Create();
                elem.secret_trigger = secret_trigger;
                elem.secret = secret;
                elem.triggerer = trigger;
                secret_queue.Enqueue(elem);
            }
        }

        //Resolve the next queued action, abilities have priorities, then secrets, attacks, callbacks..
        public virtual void Resolve()
        {
            if (ability_cast_queue.Count > 0)
            {
                //Resolve Ability
                AbilityQueueElement elem = ability_cast_queue.Dequeue();
                ability_elem_pool.Dispose(elem);
                ResolveCardAbility(elem.ability, elem.caster, elem.triggerer);
            }
            else if (secret_queue.Count > 0)
            {
                //Resolve Secret
                SecretQueueElement elem = secret_queue.Dequeue();
                secret_elem_pool.Dispose(elem);
                ResolveSecret(elem.secret_trigger, elem.secret, elem.triggerer);
            }
            else if (attack_queue.Count > 0)
            {
                //Resolve Attack
                AttackQueueElement elem = attack_queue.Dequeue();
                attack_elem_pool.Dispose(elem);
                if (elem.ptarget != null)
                    ResolveAttackPlayer(elem.attacker, elem.ptarget);
                else
                    ResolveAttack(elem.attacker, elem.target);
            }
            else if (callback_queue.Count > 0)
            {
                CallbackQueueElement elem = callback_queue.Dequeue();
                callback_elem_pool.Dispose(elem);
                elem.callback.Invoke();
            }
        }

        //Start resolving all triggered and queued abilities/attacks/secrets...
        public virtual void ResolveAll()
        {
            if (is_resolving)
                return; //Already being resolved

            is_resolving = true;
            while (CanResolve())
            {
                Resolve();
            }
            is_resolving = false;
        }

        public virtual bool CanResolve()
        {
            if (game_data.state == GameState.GameEnded)
                return false; //Cant execute anymore when game is ended
            if (game_data.selector != SelectorType.None)
                return false; //Waiting for player input, in the middle of resolve loop
            return attack_queue.Count > 0 || ability_cast_queue.Count > 0 || secret_queue.Count > 0 || callback_queue.Count > 0;
        }

        public virtual bool IsResolving()
        {
            return is_resolving;
        }

        public virtual void Clear()
        {
            attack_elem_pool.DisposeAll();
            ability_elem_pool.DisposeAll();
            secret_elem_pool.DisposeAll();
            callback_elem_pool.DisposeAll();
            attack_queue.Clear();
            ability_cast_queue.Clear();
            secret_queue.Clear();
            callback_queue.Clear();
        }

        //-------------

        public virtual bool IsGameStarted()
        {
            return game_data.HasStarted();
        }

        public virtual bool IsGameEnded()
        {
            return game_data.HasEnded();
        }

        public virtual Game GetGameData()
        {
            return game_data;
        }

        public Game GameData { get { return game_data; } }
    }
}