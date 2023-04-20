using UnityEngine;
using System.Collections;
using Utility;
using System.Collections.Generic;
using Events;
using SimpleJSONFixed;
using System;


namespace ApplicationManagers
{
    public class MiscInfo : MonoBehaviour
    {
        public static JSONNode Social;
        public static JSONNode Help;
        public static JSONNode Credits;

        public static void Init()
        {
            Social = JSON.Parse(AssetBundleManager.TryLoadText("SocialInfo"));
            Help = JSON.Parse(AssetBundleManager.TryLoadText("HelpInfo"));
            Credits = JSON.Parse(AssetBundleManager.TryLoadText("CreditsInfo"));
        }
    }
}