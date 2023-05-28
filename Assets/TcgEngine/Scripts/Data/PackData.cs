using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Defines all packs data
    /// </summary>

    [CreateAssetMenu(fileName = "PackData", menuName = "TcgEngine/PackData", order = 5)]
    public class PackData : ScriptableObject
    {
        public string id;
        public int cards = 5;   //Cards per pack
        public int cost = 100;  //Cost to buy
        public int[] rarities_1st;  //Probability of each rarity, for first card, first element is common
        public int[] rarities;      //Probability of each rarity, for other cards, first element is common

        [Header("Display")]
        public string title;
        public Sprite pack_img;
        public Sprite cardback_img;
        [TextArea(5, 10)]
        public string desc;
        public int sort_order;

        [Header("Availability")]
        public bool available = true;

        public static List<PackData> pack_list = new List<PackData>();

        public static void Load(string folder = "")
        {
            if (pack_list.Count == 0)
                pack_list.AddRange(Resources.LoadAll<PackData>(folder));

            pack_list.Sort((PackData a, PackData b) => {
                if (a.sort_order == b.sort_order)
                    return a.id.CompareTo(b.id);
                else
                    return a.sort_order.CompareTo(b.sort_order);
            });
        }

        public string GetTitle()
        {
            return title;
        }

        public string GetDesc()
        {
            return desc;
        }

        public static PackData Get(string id)
        {
            foreach (PackData pack in GetAll())
            {
                if (pack.id == id)
                    return pack;
            }
            return null;
        }

        public static List<PackData> GetAllAvailable()
        {
            List<PackData> valid_list = new List<PackData>();
            foreach (PackData apack in GetAll())
            {
                if (apack.available)
                    valid_list.Add(apack);
            }
            return valid_list;
        }

        public static List<PackData> GetAll()
        {
            return pack_list;
        }
    }
}