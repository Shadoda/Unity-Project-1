using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TcgEngine.Client;
using TcgEngine;

namespace TcgEngine.UI
{
    /// <summary>
    /// Matchmaking panel is just a loading panel that displays how many players are found yet
    /// </summary>

    public class MatchmakingPanel : UIPanel
    {
        public Text text;
        public Text players_txt;

        private static MatchmakingPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Start()
        {
            base.Start();

        }

        protected override void Update()
        {
            base.Update();

            if (GameClientMatchmaker.Get().IsConnected())
                text.text = "Finding Opponent...";
            else
                text.text = "Connecting to server...";
        }

        public void SetCount(int players)
        {
            if (players_txt != null)
                players_txt.text = players.ToString() + "/" + GameClientMatchmaker.Get().GetNbPlayers();
        }

        public void OnClickCancel()
        {
            GameClientMatchmaker.Get().StopMatchmaking();
            Hide();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            if (players_txt != null)
                players_txt.text = "";
        }

        public static MatchmakingPanel Get()
        {
            return instance;
        }
    }
}