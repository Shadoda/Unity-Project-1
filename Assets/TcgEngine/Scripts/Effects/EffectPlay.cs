using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect to play a card from your hand for free
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Play", order = 10)]
    public class EffectPlay : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game game = logic.GetGameData();
            Player player = game.GetPlayer(target.player_id);
            Slot slot = player.GetRandomEmptySlot();
            if (slot != Slot.None)
            {
                logic.PlayCard(target, slot, true);
            }
        }
    }
}