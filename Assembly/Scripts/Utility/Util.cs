using Settings;
using SimpleJSONFixed;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Characters;
using System.Text.RegularExpressions;

namespace Utility
{
    static class Util
    {
        public static void DontDestroyOnLoad(GameObject obj)
        {
            UnityEngine.Object.DontDestroyOnLoad(obj);
            obj.AddComponent<DontDestroyOnLoadTag>();
        }

        public static BaseCharacter FindCharacterByViewId(int viewId)
        {
            if (viewId < 0)
                return null;
            var view = PhotonView.Find(viewId);
            if (view == null)
                return null;
            var character = view.GetComponent<BaseCharacter>();
            return character;
        }

        public static PhotonPlayer FindPlayerById(int id)
        {
            foreach (var player in PhotonNetwork.playerList)
            {
                if (player.ID == id)
                    return player;
            }
            return null;
        }

        public static string PascalToSentence(string str)
        {
            return Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");
        }

        public static T CreateDontDestroyObj<T>() where T : Component
        {
            GameObject obj = new GameObject();
            obj.name = "Dont Destroy";
            Util.DontDestroyOnLoad(obj);
            return obj.AddComponent<T>();
        }

        public static T CreateObj<T>() where T : Component
        {
            GameObject obj = new GameObject();
            return obj.AddComponent<T>();
        }

        public static HashSet<T> RemoveNull<T>(HashSet<T> set) where T: UnityEngine.Object
        {
            HashSet<T> newSet = new HashSet<T>();
            foreach (T item in set)
            {
                if (item != null)
                    newSet.Add(item);
            }
            return newSet;
        }

        public static HashSet<T> RemoveNullOrDead<T>(HashSet<T> set) where T : BaseCharacter
        {
            HashSet<T> newSet = new HashSet<T>();
            foreach (T item in set)
            {
                if (item != null && !item.Dead)
                    newSet.Add(item);
            }
            return newSet;
        }

        public static HashSet<BaseShifter> RemoveNullOrDeadShifters(HashSet<BaseShifter> set)
        {
            HashSet<BaseShifter> newSet = new HashSet<BaseShifter>();
            foreach (BaseShifter item in set)
            {
                if (item != null && (!item.Dead || item.TransformingToHuman))
                    newSet.Add(item);
            }
            return newSet;
        }

        public static string CreateMD5(string input)
        {
            if (input == string.Empty)
                return string.Empty;
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static IEnumerator WaitForFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return new WaitForEndOfFrame();
        }

        public static string[] EnumToStringArray<T>()
        {
            return Enum.GetNames(typeof(T));
        }

        public static string[] EnumToStringArrayExceptNone<T>()
        {
            List<string> names = new List<string>();
            foreach (string str in EnumToStringArray<T>())
            {
                if (str != "None")
                    names.Add(str);
            }
            return names.ToArray();
        }

        public static List<T> EnumToList<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }

        public static Dictionary<string, T> EnumToDict<T>()
        {
            Dictionary<string, T> dict = new Dictionary<string, T>();
            foreach (T t in EnumToList<T>())
            {
                dict.Add(t.ToString(), t);
            }
            return dict;
        }

        public static string FormatFloat(float num, int decimalPlaces)
        {
            if (decimalPlaces == 0)
                return num.ToString("0");
            return num.ToString("0." + new string('0', decimalPlaces));
        }

        public static Vector3 MultiplyVectors(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static Vector3 DivideVectors(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        public static List<List<T>> GroupItems<T>(List<T> items, int groupSize)
        {
            var list = new List<List<T>>();
            if (items.Count == 0)
                return list;
            var group = new List<T>();
            list.Add(group);
            int current = 0;
            foreach (var obj in items)
            {
                current++;
                if (current >= groupSize + 1)
                {
                    current = 1;
                    group = new List<T>();
                    list.Add(group);
                }
                group.Add(obj);
            }
            return list;
        }

        public static object GetRandomFromWeightedList(List<object> values, List<float> weights)
        {
            float totalWeight = 0f;
            foreach (float w in weights)
                totalWeight += w;
            float r = UnityEngine.Random.Range(0f, totalWeight);
            float start = 0f;
            for (int i = 0; i < values.Count; i++)
            {
                if (r >= start && r < start + weights[i])
                    return values[i];
                start += weights[i];
            }
            return values[0];
        }

        public static object GetRandomFromWeightedNode(JSONNode node)
        {
            List<object> values = new List<object>();
            List<float> weights = new List<float>();
            foreach (JSONNode key in node.Keys)
            {
                values.Add(key.Value);
                weights.Add(node[key.Value].AsFloat);
            }
            return GetRandomFromWeightedList(values, weights);
        }

        public static float DistanceIgnoreY(Vector3 a, Vector3 b)
        {
            a = new Vector3(a.x, 0f, a.z);
            b = new Vector3(b.x, 0f, b.z);
            return Vector3.Distance(a, b);
        }
    }

    class DontDestroyOnLoadTag : MonoBehaviour
    {
    }
}