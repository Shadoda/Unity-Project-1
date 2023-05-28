using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that damages a card or a player (lose hp)
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Damage", order = 10)]
    public class EffectDamage : EffectData
    {
        public TraitData bonus_damage;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            int damage = GetDamage(logic.GameData, caster, ability.value);
            target.hp -= damage;
            target.hp = Mathf.Clamp(target.hp, 0, target.hp_max);
            //At the end of turn, CheckForWinner will check if player is dead
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int damage = GetDamage(logic.GameData, caster, ability.value);
            logic.DamageCard(caster, target, damage);
        }

        private int GetDamage(Game data, Card caster, int value)
        {
            Player player = data.GetPlayer(caster.player_id);
            int damage = value + caster.GetStatValue(bonus_damage) + player.GetStatValue(bonus_damage);
            return damage;
        }

        public override int GetAiValue(AbilityData ability)
        {
            return -1;
        }
    }
}