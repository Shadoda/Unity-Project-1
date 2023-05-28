using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// Main player UI inside the GameUI, inside the game scene
    /// there is one for each player
    /// </summary>

    public class PlayerUI : MonoBehaviour
    {
        public bool is_opponent;
        public Text pname;
        public AvatarUI avatar;
        public IconBar mana_bar;
        public Text hp_txt;
        public Text hp_max_txt;

        public Animator[] secrets;

        public GameObject dead_fx;
        public AudioClip dead_audio;
        public Sprite avatar_dead;

        private bool killed = false;

        private static List<PlayerUI> ui_list = new List<PlayerUI>();

        private void Awake()
        {
            ui_list.Add(this);
        }

        private void OnDestroy()
        {
            ui_list.Remove(this);
        }

        void Start()
        {
            pname.text = "";
            hp_txt.text = "";
            hp_max_txt.text = "";
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            int player_id = is_opponent ? GameClient.Get().GetOpponentPlayerID() : GameClient.Get().GetPlayerID();
            Game data = GameClient.Get().GetGameData();
            Player player = data.GetPlayer(player_id);

            if (player != null)
            {
                pname.text = player.username;
                mana_bar.value = player.mana;
                mana_bar.max_value = player.mana_max;
                hp_txt.text = player.hp.ToString();
                hp_max_txt.text = "/" + player.hp_max.ToString();

                AvatarData adata = AvatarData.Get(player.avatar);
                if (avatar != null && adata != null && !killed)
                    avatar.SetAvatar(adata);

                for (int i = 0; i < secrets.Length; i++)
                {
                    bool active = i < player.cards_secret.Count;
                    bool was_active = secrets[i].gameObject.activeSelf;
                    if (active != was_active)
                        secrets[i].gameObject.SetActive(active);
                    if (active && !was_active)
                        secrets[i].SetTrigger("fx");
                }
            }
        }

        public void Kill()
        {
            killed = true;
            avatar.SetImage(avatar_dead);
            AudioTool.Get().PlaySFX("fx", dead_audio);
            FXTool.DoFX(dead_fx, avatar.transform.position);
        }

        public static PlayerUI Get(bool opponent)
        {
            foreach (PlayerUI ui in ui_list)
            {
                if (ui.is_opponent == opponent)
                    return ui;
            }
            return null;
        }

    }
}