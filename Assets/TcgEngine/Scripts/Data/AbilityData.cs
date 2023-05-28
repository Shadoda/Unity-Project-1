using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Defines all ability data
    /// </summary>

    [CreateAssetMenu(fileName = "ability", menuName = "TcgEngine/AbilityData", order = 4)]
    public class AbilityData : ScriptableObject
    {
        public string id;

        [Header("Trigger")]
        public AbilityTrigger trigger;             //WHEN does the ability trigger?
        public ConditionData[] conditions_trigger; //Condition checked on the card triggering the ability (usually the caster)

        [Header("Target")]
        public AbilityTarget target;               //WHO is targeted?
        public ConditionData[] conditions_target;  //Condition checked on the target to know if its a valid taget
        public FilterData[] filters_target;  //Condition checked on the target to know if its a valid taget

        [Header("Effect")]
        public EffectData[] effects;              //WHAT this does?
        public StatusData[] status;               //Status added by this ability  
        public int value;                         //Value passed to the effect (deal X damage)
        public int duration;                      //Duration passed to the effect (usually for status, 0=permanent)

        [Header("Chain/Choices")]
        public AbilityData[] chain_abilities;    //Abilities that will be triggered after this one

        [Header("Activated Ability")]
        public int mana_cost;                   //Mana cost for  activated abilities
        public bool exhaust;                    //Action cost for activated abilities

        [Header("FX")]
        public GameObject board_fx;
        public GameObject caster_fx;
        public GameObject target_fx;
        public AudioClip cast_audio;
        public AudioClip target_audio;
        public bool charge_target;

        [Header("Text")]
        public string title;
        [TextArea(5, 7)]
        public string desc;

        public static List<AbilityData> ability_list = new List<AbilityData>();

        public static void Load(string folder = "")
        {
            if (ability_list.Count == 0)
                ability_list.AddRange(Resources.LoadAll<AbilityData>(folder));
        }

        public string GetTitle()
        {
            return title;
        }

        public string GetDesc()
        {
            return desc;
        }

        public string GetDesc(CardData card)
        {
            string dsc = desc;
            dsc = dsc.Replace("<name>", card.title);
            dsc = dsc.Replace("<value>", value.ToString());
            dsc = dsc.Replace("<duration>", duration.ToString());
            return dsc;
        }

        //Generic condition for the ability to trigger
        public bool AreTriggerConditionsMet(Game data, Card caster)
        {
            return AreTriggerConditionsMet(data, caster, caster); //Triggerer is the caster
        }

        //Some abilities are caused by another card (PlayOther), otherwise most of the time the triggerer is the caster, check condition on triggerer
        public bool AreTriggerConditionsMet(Game data, Card caster, Card trigger_card)
        {
            foreach (ConditionData cond in conditions_trigger)
            {
                if (cond != null)
                {
                    if (!cond.IsTriggerConditionMet(data, this, caster))
                        return false;
                    if (!cond.IsTargetConditionMet(data, this, caster, trigger_card))
                        return false;
                }
            }
            return true;
        }

        //Some abilities are caused by an action on a player (OnFight when attacking the player), check condition on that player
        public bool AreTriggerConditionsMet(Game data, Card caster, Player trigger_player)
        {
            foreach (ConditionData cond in conditions_trigger)
            {
                if (cond != null)
                {
                    if (!cond.IsTriggerConditionMet(data, this, caster))
                        return false;
                    if (!cond.IsTargetConditionMet(data, this, caster, trigger_player))
                        return false;
                }
            }
            return true;
        }

        //Check if the card target is valid
        public bool AreTargetConditionsMet(Game data, Card caster, Card target_card)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond && !cond.IsTargetConditionMet(data, this, caster, target_card))
                    return false;
            }
            return true;
        }

        //Check if the player target is valid
        public bool AreTargetConditionsMet(Game data, Card caster, Player target_player)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond && !cond.IsTargetConditionMet(data, this, caster, target_player))
                    return false;
            }
            return true;
        }

        //Check if the slot target is valid
        public bool AreTargetConditionsMet(Game data, Card caster, Slot target_slot)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond && !cond.IsTargetConditionMet(data, this, caster, target_slot))
                    return false;
            }
            return true;
        }

        //CanTarget is similar to AreTargetConditionsMet but only applies to targets on the board, with extra board-only conditions
        public bool CanTarget(Game data, Card caster, Card target, bool ai_check = false)
        {
            if (target == null)
                return false;

            if (target.HasStatus(StatusType.Stealth))
                return false; //Hidden

            if (target.HasStatus(StatusType.SpellImmunity))
                return false; //Spell immunity

            if (ai_check && !CanAiTarget(data, caster, target))
                return false; //Additional AI Conditions

            bool condition_match = AreTargetConditionsMet(data, caster, target);
            return condition_match;
        }

        //Can target check additional restrictions and is usually for SelectTarget or PlayTarget abilities
        public bool CanTarget(Game data, Card caster, Player target, bool ai_check = false)
        {
            if (target == null)
                return false;

            if (ai_check && !CanAiTarget(data, caster, target))
                return false; //Additional AI Conditions

            bool condition_match = AreTargetConditionsMet(data, caster, target);
            return condition_match;
        }

        public bool CanTarget(Game data, Card caster, Slot target, bool ai_check = false)
        {
            return AreTargetConditionsMet(data, caster, target); //No additional conditions for slots
        }

        //AI has additional restrictions based on if the effect is positive or not
        public bool CanAiTarget(Game data, Card caster, Card target_card)
        {
            return CanAiTarget(data, caster, data.GetPlayer(target_card.player_id));
        }

        //AI has additional restrictions based on if the effect is positive or not
        public bool CanAiTarget(Game data, Card caster, Player target_player)
        {
            int ai_value = GetAiValue();
            if (ai_value > 0 && caster.player_id != target_player.player_id)
                return false; //Positive effect, dont target others
            if (ai_value < 0 && caster.player_id == target_player.player_id)
                return false; //Negative effect, dont target self
            return true;
        }

        public void DoEffects(GameLogic logic, Card caster)
        {
            foreach(EffectData effect in effects)
                effect.DoEffect(logic, this, caster);
        }

        public void DoEffects(GameLogic logic, Card caster, Card target)
        {
            foreach (EffectData effect in effects)
                effect.DoEffect(logic, this, caster, target);
            foreach(StatusData stat in status)
                target.AddStatus(stat.effect, value, duration);
        }

        public void DoEffects(GameLogic logic, Card caster, Player target)
        {
            foreach (EffectData effect in effects)
                effect.DoEffect(logic, this, caster, target);
            foreach (StatusData stat in status)
                target.AddStatus(stat.effect, value, duration);
        }

        public void DoEffects(GameLogic logic, Card caster, Slot target)
        {
            foreach (EffectData effect in effects)
                effect.DoEffect(logic, this, caster, target);
        }

        public void DoOngoingEffects(GameLogic logic, Card caster, Card target)
        {
            foreach (EffectData effect in effects)
                effect.DoOngoingEffect(logic, this, caster, target);
            foreach (StatusData stat in status)
                target.AddOngoingStatus(stat.effect, value);
        }

        public void DoOngoingEffects(GameLogic logic, Card caster, Player target)
        {
            foreach (EffectData effect in effects)
                effect.DoOngoingEffect(logic, this, caster, target);
            foreach (StatusData stat in status)
                target.AddOngoingStatus(stat.effect, value);
        }

        public bool HasEffect<T>() where T : EffectData
        {
            foreach (EffectData eff in effects)
            {
                if (eff is T)
                    return true;
            }
            return false;
        }

        public int GetAiValue()
        {
            int total = 0;
            foreach (EffectData eff in effects)
                total += eff.GetAiValue(this);
            foreach (StatusData astatus in status)
                total += astatus.hvalue;
            foreach (AbilityData ability in chain_abilities)
                total += ability.GetAiValue();
            return total;
        }

        //Return list of possible targets (not selected yet),  memory_array is used for optimization and avoid allocating new memory
        public List<Card> GetValidCardTargets(Game data, Card caster, List<Card> memory_array = null)
        {
            List<Card> valid_targets = memory_array != null ? memory_array : new List<Card>();
            if (valid_targets.Count > 0)
                valid_targets.Clear();

            //Card can be anywhere
            if (target == AbilityTarget.AllCardsAllPiles || target == AbilityTarget.CardSelector)
            {
                foreach (Player player in data.players)
                {
                    foreach (Card card in player.cards_all.Values)
                    {
                        if (AreTargetConditionsMet(data, caster, card))
                            valid_targets.Add(card);
                    }
                }
            }
            //Only cards on board
            else
            {
                foreach (Player player in data.players)
                {
                    foreach (Card card in player.cards_board)
                    {
                        if (AreTargetConditionsMet(data, caster, card))
                            valid_targets.Add(card);
                    }
                }
            }

            return valid_targets;
        }

        //Return cards targets,  memory_array is used for optimization and avoid allocating new memory
        public List<Card> GetCardTargets(Game data, Card caster, List<Card> memory_array = null)
        {
            List<Card> targets = memory_array != null ? memory_array : new List<Card>();
            if (targets.Count > 0)
                targets.Clear();

            if (target == AbilityTarget.Self)
            {
                if (AreTargetConditionsMet(data, caster, caster))
                    targets.Add(caster);
            }

            if (target == AbilityTarget.AllCardsBoard)
            {
                foreach (Player player in data.players)
                {
                    foreach (Card card in player.cards_board)
                    {
                        if (AreTargetConditionsMet(data, caster, card))
                            targets.Add(card);
                    }
                }
            }

            if (target == AbilityTarget.AllCardsAllPiles)
            {
                foreach (Player player in data.players)
                {
                    foreach (Card card in player.cards_all.Values)
                    {
                        if (AreTargetConditionsMet(data, caster, card))
                            targets.Add(card);
                    }
                }
            }

            if (target == AbilityTarget.LastPlayed)
            {
                Card target = data.last_played;
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            if (target == AbilityTarget.LastKilled)
            {
                Card target = data.last_killed;
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            if (target == AbilityTarget.LastTargeted)
            {
                Card target = data.last_target;
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            if (target == AbilityTarget.AbilityTriggerer)
            {
                Card target = data.ability_triggerer;
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            return targets;
        }

        //Return player targets,  memory_array is used for optimization and avoid allocating new memory
        public List<Player> GetPlayerTargets(Game data, Card caster, List<Player> memory_array = null)
        {
            List<Player> targets = memory_array != null ? memory_array : new List<Player>();
            if (targets.Count > 0)
                targets.Clear();

            if (target == AbilityTarget.PlayerSelf)
            {
                Player player = data.GetPlayer(caster.player_id);
                targets.Add(player);
            }
            else if (target == AbilityTarget.PlayerOpponent)
            {
                for (int tp = 0; tp < data.players.Length; tp++)
                {
                    if (tp != caster.player_id)
                    {
                        Player oplayer = data.players[tp];
                        targets.Add(oplayer);
                    }
                }
            }
            else if (target == AbilityTarget.AllPlayers)
            {
                targets.AddRange(data.players);
            }

            return targets;
        }

        //Return slot targets,  memory_array is used for optimization and avoid allocating new memory
        public List<Slot> GetSlotTargets(Game data, Card caster, List<Slot> memory_array = null)
        {
            List<Slot> targets = memory_array != null ? memory_array : new List<Slot>();
            if (targets.Count > 0)
                targets.Clear();

            if (target == AbilityTarget.AllSlots)
            {
                List<Slot> slots = Slot.GetAll();
                foreach (Slot slot in slots)
                {
                    if (AreTargetConditionsMet(data, caster, slot))
                        targets.Add(slot);
                }
            }

            return targets;
        }

        public bool IsSelectTarget()
        {
            return target == AbilityTarget.SelectTarget;
        }

        public bool IsSelector()
        {
            return target == AbilityTarget.CardSelector || target == AbilityTarget.ChoiceSelector;
        }

        public static AbilityData Get(string id)
        {
            foreach (AbilityData ability in GetAll())
            {
                if (ability.id == id)
                    return ability;
            }
            return null;
        }

        public static List<AbilityData> GetAll()
        {
            return ability_list;
        }
    }


    public enum AbilityTrigger
    {
        None = 0,

        Ongoing = 2,  //Always active (does not work with all effects)
        Activate = 5, //Action

        OnPlay = 10,  //When playeds
        OnPlayOther = 12,  //When another card played

        StartOfTurn = 20, //Every turn
        EndOfTurn = 22, //Every turn

        OnBeforeAttack = 30, //When attacking, before damage
        OnAfterAttack = 31, //When attacking, after damage if still alive
        OnBeforeDefend = 32, //When being attacked, before damage
        OnAfterDefend = 33, //When being attacked, after damage if still alive
        OnKill = 35,        //When killing another card during an attack

        OnDeath = 40, //When dying
        OnDeathOther = 42, //When another dying
    }

    public enum AbilityTarget
    {
        None = 0,
        Self = 1,

        PlayerSelf = 4,
        PlayerOpponent = 5,
        AllPlayers = 7,

        AllCardsBoard = 10,
        AllCardsAllPiles = 12,
        AllSlots = 15,

        PlayTarget = 20,        //The target selected at the same time the spell was played (spell only)      
        AbilityTriggerer = 25,   //The card that triggered the trap

        SelectTarget = 30,        //Select a card, player or slot on board
        CardSelector = 40,          //Card selector menu
        ChoiceSelector = 44,        //Choice selector menu

        LastPlayed = 70,            //Last card that was played
        LastTargeted = 72,          //Last card that was targeted with an ability
        LastKilled = 74,            //Last card that was killed

    }

}
