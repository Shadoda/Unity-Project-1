using UnityEngine;
using System;
using System.Text;
using System.Security.Cryptography;
using UnityEngine.Events;

namespace TcgEngine
{
    /// <summary>
    /// Useful tool static functions for the ApiClient
    /// </summary>

    public class ApiTool : MonoBehaviour
    {
        // ----- Convertions ------

        public static T JsonToObject<T>(string json)
        {
            try
            {
                T value = JsonUtility.FromJson<T>(json);
                return value;
            }
            catch (Exception) { }

            return (T)Activator.CreateInstance(typeof(T));
        }

        public static ListResponse<T> JsonToArray<T>(string json)
        {
            ListResponse<T> list = new ListResponse<T>();
            list.list = new T[0];
            try
            {
                string wrap_json = "{ \"list\": " + json + "}";
                list = JsonUtility.FromJson<ListResponse<T>>(wrap_json);
            }
            catch (Exception) { }
            return list;
        }

        public static string ToJson(object data)
        {
            return JsonUtility.ToJson(data);
        }

        public static int Parse(string int_str, int default_val = 0)
        {
            bool success = int.TryParse(int_str, out int val);
            return success ? val : default_val;
        }
    }
}
