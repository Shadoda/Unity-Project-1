using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    //Effect to Summon an entirely new card (not in anyones deck)
    //And places it on the board

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Summon", order = 10)]
    public class EffectSummon : EffectData
    {
        public CardData summon;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            logic.SummonCard(caster.player_id, summon, target.slot); //Assumes the target has just been killed, so the slot is empty
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Slot target)
        {
            logic.SummonCard(caster.player_id, summon, target);
        }
    }
}