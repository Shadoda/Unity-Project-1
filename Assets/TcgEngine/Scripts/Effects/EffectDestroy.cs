using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Destroy", order = 10)]
    public class EffectDestroy : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            logic.KillCard(caster, target);
        }

        public override int GetAiValue(AbilityData ability)
        {
            return -1;
        }
    }
}