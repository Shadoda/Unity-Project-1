using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// Main script for the open pack scene
    /// </summary>

    public class OpenPackMenu : MonoBehaviour
    {
        public GameObject card_prefab;

        private bool revealing = false;

        private static OpenPackMenu instance;

        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (revealing && Input.GetMouseButtonDown(0))
            {
                bool all_revealed = true;
                foreach (PackCard card in PackCard.GetAll())
                {
                    if (!card.IsRevealed())
                        all_revealed = false;
                }

                if (all_revealed && PackCard.GetAll().Count > 0)
                    StopReveal();
            }
        }

        public void OpenPack(string pack_tid)
        {
            PackData pack = PackData.Get(pack_tid);
            if (pack != null)
            {
                OpenPack(pack);
            }
        }

        public void OpenPack(PackData pack)
        {
            if (Authenticator.Get().IsApi())
            {
                OpenPackApi(pack);
            }
            if (Authenticator.Get().IsTest())
            {
                OpenPackTest(pack);
            }
        }
        
        public async void OpenPackTest(PackData pack)
        {
            UserData udata = Authenticator.Get().UserData;
            if (!udata.HasPack(pack.id))
                return;

            List<UserCardData> cards = new List<UserCardData>();
            List <CardData> all_cards = CardData.GetAll(pack);

            for (int i = 0; i < pack.cards; i++)
            {
                CardData card = all_cards[Random.Range(0, all_cards.Count)];
                UserCardData ucard = new UserCardData();
                ucard.tid = UserCardData.GetTid(card.id, CardVariant.Normal);
                ucard.quantity = 1;
                cards.Add(ucard);
            }

            udata.AddPack(pack.id, -1);
            foreach (UserCardData card in cards)
            {
                udata.AddCard(card.tid, card.quantity);
            }

            await Authenticator.Get().SaveUserData();
            RevealCards(pack, cards.ToArray());
            HandPackArea.Get().LoadPacks();
        }

        public async void OpenPackApi(PackData pack)
        {
            UserData udata = Authenticator.Get().UserData;
            if (!udata.HasPack(pack.id))
                return;

            udata.AddPack(pack.id, -1);

            OpenPackRequest req = new OpenPackRequest();
            req.pack = pack.id;

            string url = ApiClient.ServerURL + "/users/packs/open";
            string json = ApiTool.ToJson(req);

            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            if (res.success)
            {
                ListResponse<UserCardData> cards = ApiTool.JsonToArray<UserCardData>(res.data);
                RevealCards(pack, cards.list);
            }

            HandPackArea.Get().LoadPacks();
        }

        public void RevealCards(PackData pack, UserCardData[] cards)
        {
            UserData udata = Authenticator.Get().UserData;
            CardbackData cb = CardbackData.Get(udata.cardback);
            HandPackArea.Get().Lock(true);
            revealing = true;

            int index = 0;
            foreach (UserCardData card in cards)
            {
                GameObject cobj = Instantiate(card_prefab, new Vector3(0f, -3f, 0f), Quaternion.identity);
                PackCard pcard = cobj.GetComponent<PackCard>();
                string card_id = UserCardData.GetCardId(card.tid);
                CardVariant variant = UserCardData.GetCardVariant(card.tid);
                CardData icard = CardData.Get(card_id);
                pcard.SetCard(pack, icard, variant);
                BoardRef bref = BoardRef.Get(BoardRefType.CardArea, index);
                Vector3 pos = bref != null ? bref.transform.position : Vector3.zero;
                pcard.SetTarget(pos);
                index++;
            }
        }

        public void StopReveal()
        {
            revealing = false;
            HandPackArea.Get().Lock(false);
            foreach (PackCard card in PackCard.GetAll())
            {
                card.Remove();
            }
        }

        public void OnClickBack()
        {
            SceneNav.GoTo("Menu");
        }

        public static OpenPackMenu Get()
        {
            return instance;
        }
    }

}