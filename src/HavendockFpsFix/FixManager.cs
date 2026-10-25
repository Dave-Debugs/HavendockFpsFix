using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal static class FixManager
    {
        internal const string HarmonyId = "havendock-fpsfix";
        internal static bool MasterOn = true;
        private static Harmony _harmony;
        private static readonly List<IFix> _fixes = new List<IFix>();
        private static readonly List<GameObject> _runners = new List<GameObject>();

        internal interface IFix
        {
            string Name { get; }
            ConfigEntry<bool> Enabled { get; }
            bool Applied { get; }
            void Bind(ConfigFile cfg);
            void Apply(Harmony harmony);
            void Restore();
            string Stats();
            void ResetStats();
        }

        internal static bool IsActive(IFix f) => MasterOn && f.Enabled.Value && f.Applied;

        internal static void Init(ConfigFile cfg)
        {
            _fixes.Clear();
            MasterOn = true;
            _fixes.Add(new NearestSpotIndex());
            _fixes.Add(new WorldCanvasCulling());
            _fixes.Add(new EmptyCanvasDormant());
            _fixes.Add(new HudNewTagToggle());
            _fixes.Add(new QualityTweaks());
            _fixes.Add(new AnimatorCulling());
            _fixes.Add(new AnimatorEvents());
            _fixes.Add(new OfflineNetSync());
            _fixes.Add(new LeanHoverCheck());
            _fixes.Add(new PostEffects());
            _fixes.Add(new WaterReflectionShadows());
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

        internal static GameObject AddRunner<T>(string name) where T : Component
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<T>();
            _runners.Add(go);
            return go;
        }

        internal static void Shutdown()
        {
            MasterOn = false;
            foreach (var f in _fixes)
            {
                try { f.Restore(); }
                catch (Exception e) { Plugin.Log.LogWarning($"{f.Name} restore: {e.Message}"); }
            }
            foreach (var go in _runners) if (go != null) UnityEngine.Object.Destroy(go);
            _runners.Clear();
            if (_harmony != null)
            {
                try
                {
                    var patched = new List<System.Reflection.MethodBase>(_harmony.GetPatchedMethods());
                    foreach (var m in patched) _harmony.Unpatch(m, HarmonyPatchType.All, HarmonyId);
                }
                catch (Exception e) { Plugin.Log.LogWarning("Unpatch: " + e.Message); }
                _harmony = null;
            }
            Plugin.Log.LogInfo("Unloaded; the game's original code is back in use.");
        }

        internal static string Describe()
        {
            return (MasterOn ? "on" : "off") + " | " +
                   string.Join(", ", _fixes.Select(f => f.Name + "=" + (!f.Applied ? "unavailable" : f.Enabled.Value ? "on" : "off")).ToArray());
        }

        internal static bool AnyActive => _fixes.Any(IsActive);
    }
}
