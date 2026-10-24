using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;

namespace HavendockFpsFix
{
    internal static class FixManager
    {
        internal const string HarmonyId = Plugin.Guid;
        internal static ConfigEntry<bool> MasterEnabled;
        private static Harmony _harmony;
        private static readonly List<IFix> _fixes = new List<IFix>();

        internal interface IFix
        {
            string Name { get; }
            ConfigEntry<bool> Enabled { get; }
            bool Applied { get; }
            void Bind(ConfigFile cfg);
            void Apply(Harmony harmony);
            string Stats();
            void ResetStats();
        }

        internal static bool IsActive(IFix f) => MasterEnabled.Value && f.Enabled.Value && f.Applied;

        internal static void Init(ConfigFile cfg)
        {
            MasterEnabled = cfg.Bind("General", "Enabled", true,
                "Master switch. false = the game runs exactly as it does without the mod.");
            _fixes.Add(new NearestSpotIndex());
            _fixes.Add(new WorldCanvasCulling());
            _fixes.Add(new EmptyCanvasDormant());
            _fixes.Add(new HudNewTagToggle());
            _fixes.Add(new QualityTweaks());
            _fixes.Add(new AnimatorCulling());
            foreach (var f in _fixes) f.Bind(cfg);
        }

        internal static void ApplyAll()
        {
            _harmony = new Harmony(HarmonyId);
            foreach (var f in _fixes)
            {
                try { f.Apply(_harmony); }
                catch (Exception e) { Plugin.Log.LogWarning($"{f.Name} could not be applied ({e.Message}); the game's original code is used for it."); }
            }
            Plugin.Log.LogInfo("Fixes: " + Describe());
        }

        internal static string Describe()
        {
            if (MasterEnabled == null) return "not started";
            return (MasterEnabled.Value ? "on" : "off") + " | " +
                   string.Join(", ", _fixes.Select(f => f.Name + "=" + (!f.Applied ? "unavailable" : f.Enabled.Value ? "on" : "off")).ToArray());
        }

        internal static bool AnyActive => _fixes.Any(IsActive);
    }
}
