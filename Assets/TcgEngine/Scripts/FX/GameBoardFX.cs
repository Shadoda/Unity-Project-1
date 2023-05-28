using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.FX
{
    /// <summary>
    /// FX that are not related to any card/player, and appear in the middle of the board
    /// Usually when big abilities are played
    /// </summary>

    public class GameBoardFX : MonoBehaviour
    {
        void Start()
        {
            GameClient.Get().onNewTurn += OnNewTurn;
            GameClient.Get().onCardPlayed += OnPlayCard;
            GameClient.Get().onAbilityStart += OnAbility;
            GameClient.Get().onSecret += OnSecret;
        }

        void OnNewTurn(int player_id)
        {
            AudioTool.Get().PlaySFX("turn", AssetData.Get().new_turn_audio);
            FXTool.DoFX(AssetData.Get().new_turn_fx, Vector3.zero);
        }

		void OnPlayCard(Card card, Slot slot)
        {
            int player_id = GameClient.Get().GetPlayerID();
            if (card != null)
            {
                CardData icard = CardData.Get(card.card_id);
                if (icard.type == CardType.Spell)
                {
                    GameObject prefab = player_id == card.player_id ? AssetData.Get().play_card_fx : AssetData.Get().play_card_other_fx;
                    GameObject obj = FXTool.DoFX(prefab, Vector3.zero);
                    CardUI ui = obj.GetComponentInChildren<CardUI>();
                    ui.SetCard(icard, card.variant);

                    AudioClip spawn_audio = icard.spawn_audio != null ? icard.spawn_audio : AssetData.Get().card_spawn_audio;
                    AudioTool.Get().PlaySFX("card_spell", spawn_audio);
                }

                if (icard.type == CardType.Secret)
                {
                    GameObject sprefab = player_id == card.player_id ? AssetData.Get().play_secret_fx : AssetData.Get().play_secret_other_fx;
                    FXTool.DoFX(sprefab, Vector3.zero);

                    AudioClip spawn_audio = icard.spawn_audio != null ? icard.spawn_audio : AssetData.Get().card_spawn_audio;
                    AudioTool.Get().PlaySFX("card_spell", spawn_audio);
                }
            }
        }

        private void OnAbility(AbilityData iability, Card caster)
        {
            if (iability != null)
            {
                FXTool.DoFX(iability.board_fx, Vector3.zero);
            }
        }

        private void OnSecret(Card secret, Card triggerer)
        {
            
        }

    }
}