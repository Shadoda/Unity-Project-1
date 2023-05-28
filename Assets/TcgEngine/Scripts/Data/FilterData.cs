using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Base class for target filters
    /// Let you filter targets after they have already been picked by conditions but before effects are applied
    /// </summary>

    public class FilterData : ScriptableObject
    {
        public virtual List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            return source; //Override this, condition targeting card
        }

        public virtual List<Player> FilterTargets(Game data, AbilityData ability, Card caster, List<Player> source, List<Player> dest)
        {
            return source; //Override this, condition targeting player
        }

        public virtual List<Slot> FilterTargets(Game data, AbilityData ability, Card caster, List<Slot> source, List<Slot> dest)
        {
            return source; //Override this, condition targeting slot
        }
    }
}
