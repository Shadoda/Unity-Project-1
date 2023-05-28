using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TcgEngine
{
    /// <summary>
    /// Generic static functions for TcgEngine
    /// </summary>

    public static class GameTool
    {
        private const string uid_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static System.Random rand = new System.Random();

        public static string GenerateRandomID(int min = 9, int max = 15)
        {
            int length = UnityEngine.Random.Range(min, max);
            string unique_id = "";
            for (int i = 0; i < length; i++)
            {
                unique_id += uid_chars[UnityEngine.Random.Range(0, uid_chars.Length - 1)];
            }
            return unique_id;
        }

        public static string GenerateRandomID(System.Random rand, int min = 9, int max = 15)
        {
            int length = rand.Next(min, max);
            string unique_id = "";
            for (int i = 0; i < length; i++)
            {
                unique_id += uid_chars[rand.Next(uid_chars.Length - 1)];
            }
            return unique_id;
        }

        public static string GenerateRandomIDFromSeed(int seed, int min = 11, int max = 17)
        {
            System.Random random = new System.Random(seed);
            int length = random.Next(min, max);
            string unique_id = "";
            for (int i = 0; i < length; i++)
            {
                unique_id += uid_chars[random.Next(uid_chars.Length - 1)];
            }
            return unique_id;
        }

        public static List<T> PickXRandom<T>(List<T> source, List<T> dest, int x)
        {
            if (source.Count <= x || x <= 0)
                return source; //No need to pick anything

            if (dest.Count > 0)
                dest.Clear();

            for (int i = 0; i < x; i++)
            {
                int r = rand.Next(source.Count);
                dest.Add(source[r]);
                source.RemoveAt(r);
            }

            return dest;
        }

        public static bool IsMobile()
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN
            return true;
#elif UNITY_WEBGL
            return WebGLTool.isMobile();
#else
            return false;
#endif
        }

        public static bool IsURP()
        {
            if (GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset)
                return true;
            return false;
        }

    }
}
