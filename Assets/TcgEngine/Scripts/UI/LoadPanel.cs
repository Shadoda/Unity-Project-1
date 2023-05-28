using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Loading panel that appears at the begining of a match, waiting for players to connect
    /// </summary>

    public class LoadPanel : UIPanel
    {
        public Text load_txt;

        private static LoadPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Start()
        {
            base.Start();

            GameClient.Get().onConnectGame += OnConnect;

            if (load_txt != null)
            {
                load_txt.text = "";

                if (IsOnline())
                    load_txt.text = "Connecting to server...";
            }
        }

        private void OnConnect()
        {
            if (load_txt != null)
            {
                if (IsOnline())
                    load_txt.text = "Waiting for other player...";
            }
        }

        public bool IsOnline()
        {
            return GameClient.game_settings.IsOnline();
        }

        public static LoadPanel Get()
        {
            return instance;
        }
    }
}
