using System;
using System.IO;
using Havendock.Modding;
using UnityEngine;

namespace HavendockFpsFix
{
    internal static class Plugin
    {
        public const string Name = "Havendock FPS Fix";
        public const string Version = "2.0.0";

        internal static ModLog Log = new ModLog(null);
        internal static IModContext Context;
        internal static ConfigFile Settings;
        internal static string Folder;
        internal static string ModId = "";
        internal static ConfigEntry<bool> ShowFpsCounter;

        internal static string GameRoot
        {
            get
            {
                try { return Path.GetDirectoryName(Application.dataPath); }
                catch { return Directory.GetCurrentDirectory(); }
            }
        }

        internal static string FindFolder(IModContext context)
        {
            try
            {
                if (context != null && context.Info != null && !string.IsNullOrEmpty(context.Info.Directory)) return context.Info.Directory;
            }
            catch { }
            try
            {
                string loc = typeof(Plugin).Assembly.Location;
                if (!string.IsNullOrEmpty(loc)) return Path.GetDirectoryName(loc);
            }
            catch { }
            return Path.Combine(Path.Combine(GameRoot, "Mods"), ModId);
        }

        internal static string FindOldCopy()
        {
            try
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string n = a.GetName().Name;
                    if (n == "HavendockFpsFix" || n == "HavendockPerf") return n;
                }
            }
            catch { }
            try
            {
                string plugins = Path.Combine(Path.Combine(GameRoot, "BepInEx"), "plugins");
                if (File.Exists(Path.Combine(Path.Combine(plugins, "HavendockFpsFix"), "HavendockFpsFix.dll"))) return "HavendockFpsFix";
                if (File.Exists(Path.Combine(Path.Combine(plugins, "HavendockPerf"), "HavendockPerf.dll"))) return "HavendockPerf";
            }
            catch { }
            return null;
        }
    }
}
