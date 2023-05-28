using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine
{

    /// <summary>
    /// This script initiates loading all the game data
    /// </summary>

    public class DataLoader : MonoBehaviour
    {
        public GameplayData data;
        public AssetData assets;

        private static DataLoader instance;

        void Awake()
        {
            instance = this;
            LoadData();
        }

        public void LoadData()
        {
            CardData.Load();
            TeamData.Load();
            RarityData.Load();
            TraitData.Load();
            PackData.Load();
            DeckData.Load();
            AbilityData.Load();
            StatusData.Load();
            AvatarData.Load();
            CardbackData.Load();
        }

        public static DataLoader Get()
        {
            return instance;
        }
    }
}