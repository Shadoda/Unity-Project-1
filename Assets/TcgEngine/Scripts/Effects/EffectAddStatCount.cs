using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that sets stats equal to a dynamic calculated value from a pile (number of cards on board/hand/deck)
    /// </summary>
    
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SetStatCustomCount", order = 10)]
    public class EffectAddStatCount : EffectData
    {
        public EffectStatType type;
        public PileType pile;

        [Header("Count Traits")]
        public CardType has_type;
        public TeamData has_team;
        public TraitData has_trait;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            int val = GetCount(logic.GetGameData(), caster);
            if (type == EffectStatType.HP)
            {
                target.hp += val;
            }

            if (type == EffectStatType.Mana)
            {
                target.mana += val;
                target.mana = Mathf.Clamp(target.mana, 0, GameplayData.Get().mana_max);
            }
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int val = GetCount(logic.GetGameData(), caster);
            if (type == EffectStatType.Attack)
                target.attack += val;
            if (type == EffectStatType.HP)
                target.hp += val;
            if (type == EffectStatType.Mana)
                target.mana += val;
        }

        private int GetCount(Game data, Card caster)
        {
            Player player = data.GetPlayer(caster.player_id);
            return CountPile(player, pile);
        }

        private int CountPile(Player player, PileType pile)
        {
            List<Card> card_pile = null;

            if (pile == PileType.Hand)
                card_pile = player.cards_hand;

            if (pile == PileType.Board)
                card_pile = player.cards_board;

            if (pile == PileType.Deck)
                card_pile = player.cards_deck;

            if (pile == PileType.Discard)
                card_pile = player.cards_discard;

            if (pile == PileType.Secret)
                card_pile = player.cards_secret;

            if (card_pile != null)
            {
                int count = 0;
                foreach (Card card in card_pile)
                {
                    CardData icard = CardData.Get(card.card_id);
                    if (IsTrait(icard))
                        count++;
                }
                return count;
            }
            return 0;
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