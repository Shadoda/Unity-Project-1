using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that adds or removes basic card/player stats such as hp, attack, mana
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddStat", order = 10)]
    public class EffectAddStat : EffectData
    {
        public EffectStatType type;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            if (type == EffectStatType.HP)
            {
                target.hp += ability.value;
                target.hp_max += ability.value;
            }

            if (type == EffectStatType.Mana)
            {
                target.mana += ability.value;
                target.mana_max += ability.value;
                target.mana = Mathf.Clamp(target.mana, 0, GameplayData.Get().mana_max);
                target.mana_max = Mathf.Clamp(target.mana_max, 0, GameplayData.Get().mana_max);
            }
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack += ability.value;
            if (type == EffectStatType.HP)
                target.hp += ability.value;
            if (type == EffectStatType.Mana)
                target.mana += ability.value;
        }

        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack_ongoing_bonus += ability.value;
            if (type == EffectStatType.HP)
                target.hp_ongoing_bonus += ability.value;
            if (type == EffectStatType.Mana)
                target.mana_ongoing_bonus += ability.value;
        }

        public override int GetAiValue(AbilityData ability)
        {
            if (type == EffectStatType.Mana)
                return 0; //Mana unclear, depend of target (good for player, bad for card)

            return Mathf.RoundToInt(Mathf.Sign(ability.value));
        }
    }

    public enum EffectStatType
    {
        None = 0,
        Attack = 10,
        HP = 20,
        Mana = 30,
    }
}