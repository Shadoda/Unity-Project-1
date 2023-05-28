using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Main config file for all network-related things
    /// Server API password is not in this file (and is in the Server scene instead) to prevent exposing it to client build
    /// </summary>

    [CreateAssetMenu(fileName = "NetworkData", menuName = "TcgEngine/NetworkData", order = 5)]
    public class NetworkData : ScriptableObject
    {
        [Header("Game Server")]
        public string url;
        public ushort port;

        [Header("Login")]
        public AuthenticatorType auth_type;

        [Header("API")]
        public string api_url;
        public bool api_https;

        public static NetworkData Get()
        {
            return TcgNetwork.Get().data;
        }
    }
}
