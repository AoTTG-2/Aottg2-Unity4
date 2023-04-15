using UnityEngine;
using System.Collections;
using Utility;
using System.Collections.Generic;
using Events;
using SimpleJSONFixed;
using System;

namespace ApplicationManagers
{
    /// <summary>
    /// Loads managed text data from pastebin.
    /// </summary>
    public class PastebinLoader : MonoBehaviour
    {
        public static JSONNode Leaderboard;
        public static JSONNode Social;
        public static JSONNode Help;
        public static JSONNode Changelog;
        public static JSONNode Credits;
        public static PastebinStatus Status = PastebinStatus.Loading;
        static PastebinLoader _instance;

        // consts
        static readonly string LeaderboardURL = "https://pastebin.com/raw/zptDi9T6";
        static readonly string SocialURL = "https://pastebin.com/raw/KJjgGSQs";
        static readonly string HelpURL = "https://pastebin.com/raw/TH7EepLD";
        static readonly string ChangelogURL = "https://pastebin.com/raw/i8SL3cY1";
        static readonly string CreditsURL = "https://pastebin.com/raw/xXYc4Gth";
        private static JSONNode[] _nodes;

        public static void Init()
        {
            _instance = SingletonFactory.CreateSingleton(_instance);
        }

        public static void LoadPastebin()
        {
            _instance.StartCoroutine(_instance.LoadPastebinCoroutine());
        }

        IEnumerator LoadPastebinCoroutine()
        {
            Status = PastebinStatus.Loading;
            string[] urls = new string[] { LeaderboardURL, SocialURL, HelpURL, ChangelogURL, CreditsURL };
            _nodes = new JSONNode[urls.Length];
            for (int i = 0; i < urls.Length; i++)
                _nodes[i] = null;
            for (int i = 0; i < urls.Length; i++)
            {
                using (WWW www = new WWW(urls[i]))
                {
                    yield return www;
                    if (www.error == null)
                    {
                        try
                        {
                            _nodes[i] = JSON.Parse(www.text);
                        }
                        catch (Exception e)
                        {
                            DebugConsole.Log("Error parsing pastebin JSON: " + e.Message);
                        }
                    }
                    else
                    {
                        Debug.Log("Failed to load pastebin link: " + www.error);
                        break;
                    }
                }
            }
            Leaderboard = _nodes[0];
            Social = _nodes[1];
            Help = _nodes[2];
            Changelog = _nodes[3];
            Credits = _nodes[4];
            if (Leaderboard != null && Social != null && Help != null && Changelog != null && Credits != null)
                Status = PastebinStatus.Loaded;
            else
                Status = PastebinStatus.Failed;
        }
    }

    public enum PastebinStatus
    {
        Loading,
        Loaded,
        Failed
    }
}