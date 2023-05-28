using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    //Represent the current state of a player during the game (data only)

    [System.Serializable]
    public class Player
    {
        public int player_id;
        public string username;
        public string avatar;
        public string cardback;
        public string deck;
        public bool is_ai = false;
        public int ai_level;

        public bool connected = false; //Connected to server and game
        public bool ready = false;     //Sent all player data, ready to play

        public int hp;
        public int hp_max;
        public int mana = 0;
        public int mana_max = 0;
        public int kill_count = 0;

        public Dictionary<string, Card> cards_all = new Dictionary<string, Card>();

        public List<Card> cards_deck = new List<Card>();
        public List<Card> cards_hand = new List<Card>();
        public List<Card> cards_board = new List<Card>();
        public List<Card> cards_discard = new List<Card>();
        public List<Card> cards_secret = new List<Card>();

        public List<CardStat> stats = new List<CardStat>();
        public List<CardStat> ongoing_stats = new List<CardStat>();

        public List<CardStatus> ongoing_status = new List<CardStatus>();
        public List<CardStatus> status_effects = new List<CardStatus>();

        public List<OrderHistory> history_list = new List<OrderHistory>();

        public Player(int id) { this.player_id = id; }

        public bool IsReady() { return ready && cards_all.Count > 0; }
        public bool IsConnected() { return connected || is_ai; }

        public virtual void CleanOngoing() { ongoing_status.Clear(); ongoing_stats.Clear(); }

        //---- Cards ---------

        public void AddCard(List<Card> card_list, Card card)
        {
            card_list.Add(card);
        }

        public void RemoveCard(List<Card> card_list, Card card)
        {
            card_list.Remove(card);
        }

        public virtual void RemoveCardFromAllGroups(Card card)
        {
            cards_deck.Remove(card);
            cards_hand.Remove(card);
            cards_board.Remove(card);
            cards_deck.Remove(card);
            cards_discard.Remove(card);
            cards_secret.Remove(card);
        }
        
        public virtual Card GetRandomCard(List<Card> card_list)
        {
            if (card_list.Count > 0)
                return card_list[Random.Range(0, card_list.Count)];
            return null;
        }

        public bool HasCard(List<Card> card_list, Card card)
        {
            return card_list.Contains(card);
        }

        public Card GetHandCard(string uid)
        {
            foreach (Card card in cards_hand)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetBoardCard(string uid)
        {
            foreach (Card card in cards_board)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetDeckCard(string uid)
        {
            foreach (Card card in cards_deck)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetDiscardCard(string uid)
        {
            foreach (Card card in cards_discard)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetSlotCard(Slot slot)
        {
            foreach (Card card in cards_board)
            {
                if (card != null && card.slot == slot)
                    return card;
            }
            return null;
        }

        public Card GetCard(string uid)
        {
            if (uid != null)
            {
                bool valid = cards_all.TryGetValue(uid, out Card card);
                if (valid)
                    return card;
            }
            return null;
        }

        public bool IsOnBoard(Card card)
        {
            return card != null && GetBoardCard(card.uid) != null;
        }


        //---- Slots ---------

        public Slot GetRandomSlot()
        {
            return Slot.GetRandom(player_id);
        }

        public virtual Slot GetRandomEmptySlot()
        {
            List<Slot> valid = GetEmptySlots();
            if (valid.Count > 0)
                return valid[Random.Range(0, valid.Count)];
            return Slot.None;
        }

        public List<Slot> GetEmptySlots()
        {
            List<Slot> valid = new List<Slot>();
            foreach (Slot slot in Slot.GetAll(player_id))
            {
                Card slot_card = GetSlotCard(slot);
                if (slot_card == null)
                    valid.Add(slot);
            }
            return valid;
        }

        //------ Custom Stats ---------

        public void SetStats(CardData icard)
        {
            stats.Clear();
            if (icard.stats != null)
            {
                foreach (TraitStat stat in icard.stats)
                    SetStat(stat.trait.id, stat.value);
            }
        }

        public void SetStat(string id, int value)
        {
            CardStat stat = GetStat(id);
            if (stat != null)
            {
                stat.value = value;
            }
            else
            {
                stat = new CardStat(id, value);
                stats.Add(stat);
            }
        }

        public void AddStat(string id, int value)
        {
            CardStat stat = GetStat(id);
            if (stat != null)
                stat.value += value;
            else
                SetStat(id, value);
        }

        public void AddOngoingStat(string id, int value)
        {
            CardStat stat = GetOngoingStat(id);
            if (stat != null)
            {
                stat.value += value;
            }
            else
            {
                stat = new CardStat(id, value);
                ongoing_stats.Add(stat);
            }
        }

        public CardStat GetStat(string id)
        {
            foreach (CardStat stat in stats)
            {
                if (stat.id == id)
                    return stat;
            }
            return null;
        }

        public CardStat GetOngoingStat(string id)
        {
            foreach (CardStat stat in ongoing_stats)
            {
                if (stat.id == id)
                    return stat;
            }
            return null;
        }

        public int GetStatValue(TraitData stat)
        {
            if (stat != null)
                return GetStatValue(stat.id);
            return 0;
        }

        public virtual int GetStatValue(string id)
        {
            int val = 0;
            CardStat stat1 = GetStat(id);
            CardStat stat2 = GetOngoingStat(id);
            if (stat1 != null)
                val += stat1.value;
            if (stat2 != null)
                val += stat2.value;
            return val;
        }

        //---- Status ---------

        public void AddStatus(StatusType effect, int value, int duration)
        {
            if (effect != StatusType.None)
            {
                CardStatus status = GetStatus(effect);
                if (status == null)
                {
                    status = new CardStatus(effect, value, duration);
                    status_effects.Add(status);
                }
                else
                {
                    status.value += value;
                    status.duration = Mathf.Max(status.duration, duration);
                    status.permanent = status.permanent || duration == 0;
                }
            }
        }

        public void AddOngoingStatus(StatusType effect, int value)
        {
            if (effect != StatusType.None)
            {
                CardStatus status = GetOngoingStatus(effect);
                if (status == null)
                {
                    status = new CardStatus(effect, value, 0);
                    ongoing_status.Add(status);
                }
                else
                {
                    status.value += value;
                }
            }
        }

        public void RemoveStatus(StatusType effect)
        {
            for (int i = status_effects.Count - 1; i >= 0; i--)
            {
                if (status_effects[i].type == effect)
                    status_effects.RemoveAt(i);
            }
        }

        public CardStatus GetStatus(StatusType effect)
        {
            foreach (CardStatus status in status_effects)
            {
                if (status.type == effect)
                    return status;
            }
            return null;
        }

        public CardStatus GetOngoingStatus(StatusType effect)
        {
            foreach (CardStatus status in ongoing_status)
            {
                if (status.type == effect)
                    return status;
            }
            return null;
        }

        public List<CardStatus> GetAllStatus()
        {
            List<CardStatus> all_status = new List<CardStatus>();
            all_status.AddRange(status_effects);
            all_status.AddRange(ongoing_status);
            return all_status;
        }

        public bool HasStatusEffect(StatusType effect)
        {
            return GetStatus(effect) != null || GetOngoingStatus(effect) != null;
        }

        public virtual int GetStatusEffectValue(StatusType effect)
        {
            CardStatus status1 = GetStatus(effect);
            CardStatus status2 = GetOngoingStatus(effect);
            return status1.value + status2.value;
        }

        //---- History ---------

        public void AddHistory(ushort type, Card card)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, Card target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.target_uid = target.uid;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, Player target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.target_id = target.player_id;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, AbilityData ability)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, AbilityData ability, Card target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            order.target_uid = target.uid;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, AbilityData ability, Player target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            order.target_id = target.player_id;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, AbilityData ability, Slot target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            order.slot = target;
            history_list.Add(order);
        }


        //---- Action Check ---------

        public virtual bool CanPayMana(Card card)
        {
            return mana >= card.GetMana();
        }

        public virtual void PayMana(Card card)
        {
            mana -= card.GetMana();
        }

        public virtual bool CanPayAbility(Card card, AbilityData ability)
        {
            bool exhaust = !card.exhausted || !ability.exhaust;
            return exhaust && mana >= ability.mana_cost;
        }

        public virtual bool IsDead()
        {
            if (cards_hand.Count == 0 && cards_board.Count == 0 && cards_deck.Count == 0)
                return true;
            if (hp <= 0)
                return true;
            return false;
        }

        //--------------------

        public static void Clone(Player source, Player dest)
        {
            dest.player_id = source.player_id;
            dest.is_ai = source.is_ai;
            dest.ai_level = source.ai_level;
            //dest.username = source.username;
            //dest.avatar = source.avatar;
            //dest.deck = source.deck;
            //dest.connected = source.connected;
            //dest.ready = source.ready;

            dest.hp = source.hp;
            dest.hp_max = source.hp_max;
            dest.mana = source.mana;
            dest.mana_max = source.mana_max;
            dest.kill_count = source.kill_count;

            Card.CloneDict(source.cards_all, dest.cards_all);
            Card.CloneListRef(dest.cards_all, source.cards_board, dest.cards_board);
            Card.CloneListRef(dest.cards_all, source.cards_hand, dest.cards_hand);
            Card.CloneListRef(dest.cards_all, source.cards_deck, dest.cards_deck);
            Card.CloneListRef(dest.cards_all, source.cards_discard, dest.cards_discard);
            Card.CloneListRef(dest.cards_all, source.cards_secret, dest.cards_secret);

            CardStatus.CloneList(source.status_effects, dest.status_effects);
            CardStatus.CloneList(source.ongoing_status, dest.ongoing_status);
        }
    }

    [System.Serializable]
    public class OrderHistory
    {
        public ushort type;
        public string card_id;
        public string card_uid;
        public string target_uid;
        public string ability_id;
        public int target_id;
        public Slot slot;
    }
}