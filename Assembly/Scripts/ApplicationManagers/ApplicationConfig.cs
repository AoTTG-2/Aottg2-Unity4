﻿using Photon;
using System;
using UnityEngine;
using System.IO;

namespace ApplicationManagers
{
    /// <summary>
    /// Config file to store project-level settings.
    /// </summary>
    class ApplicationConfig
    {
        private static readonly string DevelopmentConfigPath = Application.dataPath + "/DevelopmentConfig";
        public static bool DevelopmentMode = false;

        // kill-switch in case launcher becomes outdated: LauncherVersion.txt on server will be incremented
        // must be a float or else clients will not recognize it
        public const string LauncherVersion = "1.0";

        // must be manually changed every update
        public const string GameVersion = "10/31/2023";

        // must be manually updated every compatibility-breaking update
        public const string LobbyVersion = "10312023";

        public static void Init()
        {
            if (File.Exists(DevelopmentConfigPath))
            {
                DevelopmentMode = true;
            }
        }
    }
}
