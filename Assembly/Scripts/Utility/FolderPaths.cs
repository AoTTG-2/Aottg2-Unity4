using System.Collections.Generic;
using UnityEngine;
using System;

namespace Utility
{
    class FolderPaths
    {
        public static string Documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Aottg2";
        public static string ApplicationPath = Application.dataPath;
        public static string Resources = ApplicationPath + "/Resources";
        public static string TesterData = ApplicationPath + "/TesterData";
        public static string Settings = Documents + "/Settings";
        public static string Snapshots = Documents + "/Snapshots";
        public static string GameProgress = Documents + "/GameProgress";
        public static string CustomLogic = Documents + "/CustomLogic";
        public static string CustomMap = Documents + "/CustomMap";
    }
}
