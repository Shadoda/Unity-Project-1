using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Contain UserData retrieved from the web api database
    /// </summary>


    [System.Serializable]
    public class UserData
    {
        public string id;
        public string username;

        public string email;
        public string avatar;
        public string cardback;
        public int permission_level = 1;
        public int validation_level = 1;

        public int coins;
        public int xp;
        public int elo;

        public int matches;
        public int victories;
        public int defeats;

        public UserCardData[] cards;
        public UserCardData[] packs;
        public UserDeckData[] decks;
        public string[] rewards;
        public string[] avatars;
        public string[] cardbacks;
        public string[] friends;

        public UserData()
        {
            cards = new UserCardData[0];
            packs = new UserCardData[0];
            decks = new UserDeckData[0];
            rewards = new string[0];
            avatars = new string[0];
            cardbacks = new string[0];
            friends = new string[0];
            permission_level = 1;
        }

        public string GetAvatar()
        {
            if (avatar != null)
                return avatar;
            return "";
        }

        public string GetCardback()
        {
            if (cardback != null)
                return cardback;
            return "";
        }

        public void SetDeck(UserDeckData deck)
        {
            for(int i=0; i<decks.Length; i++)
            {
                if (decks[i].tid == deck.tid)
                {
                    decks[i] = deck;
                    return;
                }
            }

            //Not found
            List<UserDeckData> ldecks = new List<UserDeckData>(decks);
            ldecks.Add(deck);
            this.decks = ldecks.ToArray();
        }

        public UserDeckData GetDeck(string tid)
        {
            foreach (UserDeckData deck in decks)
            {
                if (deck.tid == tid)
                    return deck;
            }
            return null;
        }

        public UserCardData GetCard(string tid)
        {
            foreach (UserCardData card in cards)
            {
                if (card.tid == tid)
                    return card;
            }
            return null;
        }

        public int GetCardQuantity(string tid)
        {
            if (cards == null)
                return 0;

            foreach (UserCardData card in cards)
            {
                if (card.tid == tid)
                    return card.quantity;
            }
            return 0;
        }

        public UserCardData GetPack(string tid)
        {
            foreach (UserCardData pack in packs)
            {
                if (pack.tid == tid)
                    return pack;
            }
            return null;
        }

        public int GetPackQuantity(string tid)
        {
            if (packs == null)
                return 0;

            foreach (UserCardData pack in packs)
            {
                if (pack.tid == tid)
                    return pack.quantity;
            }
            return 0;
        }

        public int CountUniqueCards()
        {
            if (cards == null)
                return 0;

            HashSet<string> unique_cards = new HashSet<string>();
            foreach (UserCardData card in cards)
            {
                string card_id = UserCardData.GetCardId(card.tid);
                if (!unique_cards.Contains(card_id))
                    unique_cards.Add(card_id);
            }
            return unique_cards.Count;
        }

        public int CountCardType(CardVariant variant)
        {
            int value = 0;
            foreach (UserCardData card in cards)
            {
                if (UserCardData.GetCardVariant(card.tid) == variant)
                    value += 1;
            }
            return value;
        }

        public bool HasDeckCards(UserDeckData deck)
        {
            Dictionary<string, int> deck_qt = new Dictionary<string, int>();
            foreach (string card_tid in deck.cards)
            {
                if (!deck_qt.ContainsKey(card_tid))
                    deck_qt[card_tid] = 1;
                else
                    deck_qt[card_tid] += 1;
            }

            foreach (KeyValuePair<string, int> pair in deck_qt)
            {
                if (GetCardQuantity(pair.Key) < pair.Value)
                    return false;
            }

            return true;
        }

        public bool IsDeckValid(UserDeckData deck)
        {
            return HasDeckCards(deck) && deck.IsValid();
        }

        public void AddPack(string tid, int quantity)
        {
            bool found = false;
            foreach (UserCardData pack in packs)
            {
                if (pack.tid == tid)
                {
                    found = true;
                    pack.quantity += quantity;
                }
            }
            if (!found)
            {
                UserCardData npack = new UserCardData();
                npack.tid = tid;
                npack.quantity = quantity;
                List<UserCardData> apacks = new List<UserCardData>(packs);
                apacks.Add(npack);
                packs = apacks.ToArray();
            }
        }

        public void AddCard(string tid, int quantity)
        {
            bool found = false;
            foreach (UserCardData card in cards)
            {
                if (card.tid == tid)
                {
                    found = true;
                    card.quantity += quantity;
                }
            }
            if (!found)
            {
                UserCardData npack = new UserCardData();
                npack.tid = tid;
                npack.quantity = quantity;
                List<UserCardData> acards = new List<UserCardData>(cards);
                acards.Add(npack);
                cards = acards.ToArray();
            }
        }

        public bool HasCard(string card_tid, int quantity = 1)
        {
            foreach (UserCardData card in cards)
            {
                if (card.tid == card_tid && card.quantity >= quantity)
                    return true;
            }
            return false;
        }

        public bool HasPack(string pack_tid, int quantity=1)
        {
            foreach (UserCardData pack in packs)
            {
                if (pack.tid == pack_tid && pack.quantity >= quantity)
                    return true;
            }
            return false;
        }

        public bool HasReward(string reward_id)
        {
            foreach (string reward in rewards)
            {
                if (reward == reward_id)
                    return true;
            }
            return false;
        }

        public string GetCoinsString()
        {
            return coins.ToString();
        }

        public bool HasFriend(string username)
        {
            List<string> flist = new List<string>(friends);
            return flist.Contains(username);
        }

        public void AddFriend(string username)
        {
            List<string> flist = new List<string>(friends);
            if (!flist.Contains(username))
                flist.Add(username);
            friends = flist.ToArray();
        }

        public void RemoveFriend(string username)
        {
            List<string> flist = new List<string>(friends);
            if (flist.Contains(username))
                flist.Remove(username);
            friends = flist.ToArray();
        }
    }

    [System.Serializable]
    public class UserCardData
    {
        public string tid;
        public int quantity;

        public static string GetTid(string card_id, CardVariant variant)
        {
            if (variant == CardVariant.Foil)
                return card_id + "_foil";
            return card_id;
        }

        public static string GetCardId(string tid)
        {
            if (tid.EndsWith("_nft_foil"))
                return tid.Replace("_nft_foil", "");
            if (tid.EndsWith("_nft"))
                return tid.Replace("_nft", "");
            if (tid.EndsWith("_foil"))
                return tid.Replace("_foil", "");
            return tid;
        }

        public static CardVariant GetCardVariant(string tid)
        {
            if (tid.EndsWith("_foil"))
                return CardVariant.Foil;
            return CardVariant.Normal;
        }

        public static bool IsFoil(string tid)
        {
            CardVariant variant = GetCardVariant(tid);
            return variant == CardVariant.Foil;
        }
    }

    [System.Serializable]
    public class UserDeckData
    {
        public string tid;
        public string title;
        public string[] cards;

        public int GetQuantity()
        {
            return cards.Length;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(tid) && !string.IsNullOrWhiteSpace(title) && cards.Length >= GameplayData.Get().deck_size;
        }
    }

    [System.Serializable]
    public class UserChallengesData
    {
        public string username;
        public string time;
        public string game;
        public bool accepted;
    }
}

