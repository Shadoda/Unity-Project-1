using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    //Represent the current state of a card during the game (data only)

    [System.Serializable]
    public class Card
    {
        public string card_id;
        public string uid;
        public int player_id;
        public CardVariant variant;

        public Slot slot;
        public bool exhausted;
        public int damage = 0;

        public int mana = 0;
        public int attack = 0;
        public int hp = 0;

        public int mana_ongoing_bonus = 0;
        public int attack_ongoing_bonus = 0;
        public int hp_ongoing_bonus = 0;

        public List<CardStat> stats = new List<CardStat>();
        public List<CardStat> ongoing_stats = new List<CardStat>();

        public List<CardStatus> status = new List<CardStatus>();
        public List<CardStatus> ongoing_status = new List<CardStatus>();

        [System.NonSerialized] private CardData data = null;
        [System.NonSerialized] private int hash = 0;

        public Card(string card_id, string uid, int player_id) { this.card_id = card_id; this.uid = uid; this.player_id = player_id; }

        public virtual void Refresh() { exhausted = false; }
        public virtual void CleanOngoing() { ongoing_status.Clear(); ongoing_stats.Clear(); attack_ongoing_bonus = 0; hp_ongoing_bonus = 0; mana_ongoing_bonus = 0; }
        public virtual void Cleanse()
        {
            CleanOngoing(); Refresh(); attack = 0; hp = 0; damage = 0;
            status.Clear(); ongoing_status.Clear(); ongoing_stats.Clear();
        }

        public virtual int GetAttack() { return Mathf.Max(attack + attack_ongoing_bonus, 0); }
        public virtual int GetHP() { return Mathf.Max(hp + hp_ongoing_bonus - damage, 0); }
        public virtual int GetHPMax() { return Mathf.Max(hp + hp_ongoing_bonus, 0); }
        public virtual int GetMana() { return Mathf.Max(mana + mana_ongoing_bonus, 0); }

        public virtual void SetCard(CardData icard)
        {
            data = icard;
            card_id = icard.id;
            variant = CardVariant.Normal;
            attack = icard.attack;
            hp = icard.hp;
            mana = icard.mana;
            SetStats(icard);
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

        //------  Status Effects ---------

        public void AddStatus(StatusType type, int value, int duration)
        {
            if (type != StatusType.None)
            {
                CardStatus status = GetStatus(type);
                if (status == null)
                {
                    status = new CardStatus(type, value, duration);
                    this.status.Add(status);
                }
                else
                {
                    status.value += value;
                    status.duration = Mathf.Max(status.duration, duration);
                    status.permanent = status.permanent || duration == 0;
                }
            }
        }

        public void AddOngoingStatus(StatusType type, int value)
        {
            if (type != StatusType.None)
            {
                CardStatus status = GetOngoingStatus(type);
                if (status == null)
                {
                    status = new CardStatus(type, value, 0);
                    ongoing_status.Add(status);
                }
                else
                {
                    status.value += value;
                }
            }
        }

        public void RemoveStatus(StatusType type)
        {
            for (int i = status.Count - 1; i >= 0; i--)
            {
                if (status[i].type == type)
                    status.RemoveAt(i);
            }
        }

        public List<CardStatus> GetAllStatus()
        {
            List<CardStatus> all_status = new List<CardStatus>();
            all_status.AddRange(status);
            all_status.AddRange(ongoing_status);
            return all_status;
        }

        public bool HasStatus(StatusType type)
        {
            return GetStatus(type) != null || GetOngoingStatus(type) != null;
        }

        public CardStatus GetStatus(StatusType type)
        {
            foreach (CardStatus status in status)
            {
                if (status.type == type)
                    return status;
            }
            return null;
        }

        public CardStatus GetOngoingStatus(StatusType type)
        {
            foreach (CardStatus status in ongoing_status)
            {
                if (status.type == type)
                    return status;
            }
            return null;
        }

        public virtual int GetStatusValue(StatusType type)
        {
            CardStatus status1 = GetStatus(type);
            CardStatus status2 = GetOngoingStatus(type);
            int v1 = status1 != null ? status1.value : 0;
            int v2 = status2 != null ? status2.value : 0;
            return v1 + v2;
        }
        
        //----- Abilities ------------

        public AbilityData GetAbility(AbilityTrigger trigger)
        {
            foreach (AbilityData iability in CardData.abilities)
            {
                if (iability.trigger == trigger)
                    return iability;
            }
            return null;
        }

        public bool HasAbility(AbilityData ability)
        {
            foreach (AbilityData iability in CardData.abilities)
            {
                if (iability == ability)
                    return true;
            }
            return false;
        }

        public bool HasAbility(AbilityTrigger trigger)
        {
            AbilityData iability = GetAbility(trigger);
            if (iability != null)
                return true;
            return false;
        }

        public bool HasActiveAbility(Game data, AbilityTrigger trigger)
        {
            AbilityData iability = GetAbility(trigger);
            if (iability != null && CanDoAbilities() && iability.AreTriggerConditionsMet(data, this))
                return true;
            return false;
        }

        //---- Action Check ---------

        public virtual bool CanAttack()
        {
            if (HasStatus(StatusType.Paralysed))
                return false;
            if (exhausted)
                return false; //no more action
            return true;
        }

        public virtual bool CanMove()
        {
            //In demo we can move freely, since it has no effect
            //if (HasStatusEffect(StatusEffect.Paralysed))
            //   return false;
            //if (exhausted)
            //    return false; //no more action
            return true; 
        }

        public virtual bool CanDoActivatedAbilities()
        {
            if (HasStatus(StatusType.Paralysed))
                return false;
            if (HasStatus(StatusType.Silenced))
                return false;

            return true;
        }

        public virtual bool CanDoAbilities()
        {
            if (HasStatus(StatusType.Silenced))
                return false;
            return true;
        }

        public virtual bool CanDoAnyAction()
        {
            return CanAttack() || CanMove() || CanDoActivatedAbilities();
        }

        //----------------

        public CardData CardData 
        { 
            get { 
                if(data == null || data.id != card_id)
                    data = CardData.Get(card_id); //Optimization, store for future use
                return data;
            } 
        }

        public CardData Data => CardData; //Alternate name

        public int Hash
        {
            get {
                if (hash == 0)
                    hash = Mathf.Abs(uid.GetHashCode()); //Optimization, store for future use
                return hash;
            }
        }

        public static Card Create(string card_id, int player_id)
        {
            return Create(card_id, player_id, GameTool.GenerateRandomID(11, 15));
        }

        public static Card Create(string card_id, int player_id, string uid)
        {
            Card card = new Card(card_id, uid, player_id);
            card.SetCard(card.CardData);
            return card;
        }

        public static Card CloneNew(Card source)
        {
            Card card = new Card(source.card_id, source.uid, source.player_id);
            Clone(source, card);
            return card;
        }

        public static void Clone(Card source, Card dest)
        {
            dest.card_id = source.card_id;
            dest.uid = source.uid;
            dest.player_id = source.player_id;

            dest.variant = source.variant;
            dest.slot = source.slot;
            dest.exhausted = source.exhausted;
            dest.damage = source.damage;

            dest.attack = source.attack;
            dest.hp = source.hp;
            dest.mana = source.mana;

            dest.mana_ongoing_bonus = source.mana_ongoing_bonus;
            dest.attack_ongoing_bonus = source.attack_ongoing_bonus;
            dest.hp_ongoing_bonus = source.hp_ongoing_bonus;

            CardStat.CloneList(source.stats, dest.stats);
            CardStat.CloneList(source.ongoing_stats, dest.ongoing_stats);
            CardStatus.CloneList(source.status, dest.status);
            CardStatus.CloneList(source.ongoing_status, dest.ongoing_status);
        }

        //Clone dictionary completely
        public static void CloneDict(Dictionary<string, Card> source, Dictionary<string, Card> dest)
        {
            foreach (KeyValuePair<string, Card> pair in source)
            {
                bool valid = dest.TryGetValue(pair.Key, out Card val);
                if (valid)
                    Clone(pair.Value, val);
                else
                    dest[pair.Key] = CloneNew(pair.Value);
            }
        }

        //Clone list by keeping references from ref_dict
        public static void CloneListRef(Dictionary<string, Card> ref_dict, List<Card> source, List<Card> dest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                Card scard = source[i];
                bool valid = ref_dict.TryGetValue(scard.uid, out Card rcard);
                if (valid)
                {
                    if (i < dest.Count)
                        dest[i] = rcard;
                    else
                        dest.Add(rcard);
                }
            }

            if(dest.Count > source.Count)
                dest.RemoveRange(source.Count, dest.Count - source.Count);
        }
    }

    [System.Serializable]
    public class CardStatus
    {
        public StatusType type;
        public int value;
        public int duration = 1;
        public bool permanent = true;

        [System.NonSerialized]
        private StatusData data = null;

        public CardStatus(StatusType type, int value, int duration)
        {
            this.type = type;
            this.value = value;
            this.duration = duration;
            this.permanent = (duration == 0);
        }

        public StatusData StatusData { 
            get
            {
                if (data == null || data.effect != type)
                    data = StatusData.Get(type);
                return data;
            }
        }

        public StatusData Data => StatusData; //Alternate name

        public static CardStatus CloneNew(CardStatus copy)
        {
            CardStatus status = new CardStatus(copy.type, copy.value, copy.duration);
            status.permanent = copy.permanent;
            return status;
        }

        public static void Clone(CardStatus source, CardStatus dest)
        {
            dest.type = source.type;
            dest.value = source.value;
            dest.duration = source.duration;
            dest.permanent = source.permanent;
        }

        public static void CloneList(List<CardStatus> source, List<CardStatus> dest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (i < dest.Count)
                    Clone(source[i], dest[i]);
                else
                    dest.Add(CloneNew(source[i]));
            }

            if (dest.Count > source.Count)
                dest.RemoveRange(source.Count, dest.Count - source.Count);
        }
    }

    [System.Serializable]
    public class CardStat
    {
        public string id;
        public int value;

        [System.NonSerialized]
        private TraitData data = null;

        public CardStat(string id, int value)
        {
            this.id = id;
            this.value = value;
        }

        public CardStat(TraitData trait, int value)
        {
            this.id = trait.id;
            this.value = value;
        }

        public TraitData TraitData
        {
            get
            {
                if (data == null || data.id != id)
                    data = TraitData.Get(id);
                return data;
            }
        }

        public TraitData Data => TraitData; //Alternate name


        public static CardStat CloneNew(CardStat copy)
        {
            CardStat status = new CardStat(copy.id, copy.value);
            return status;
        }

        public static void Clone(CardStat source, CardStat dest)
        {
            dest.id = source.id;
            dest.value = source.value;
        }

        public static void CloneList(List<CardStat> source, List<CardStat> dest)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (i < dest.Count)
                    Clone(source[i], dest[i]);
                else
                    dest.Add(CloneNew(source[i]));
            }

            if (dest.Count > source.Count)
                dest.RemoveRange(source.Count, dest.Count - source.Count);
        }
    }
}
