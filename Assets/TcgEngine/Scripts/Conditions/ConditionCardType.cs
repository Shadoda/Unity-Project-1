using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Condition that checks the type, team and traits of a card
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardType", order = 10)]
    public class ConditionCardType : ConditionData
    {
        [Header("Card is of type")]
        public CardType has_type;
        public TeamData has_team;
        public TraitData has_trait;

        public ConditionOperatorBool oper;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            CardData icard = CardData.Get(target.card_id);
            return CompareBool(IsTrait(icard), oper);
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return false; //Not a card
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return false; //Not a card
        }

        private bool IsTrait(CardData icard)
        {
            bool is_type = icard.type == has_type || has_type == CardType.None;
            bool is_team = icard.team == has_team || has_team == null;
            bool is_trait = icard.HasTrait(has_trait) || has_trait == null;
            return (is_type && is_team && is_trait);
        }
    }
}