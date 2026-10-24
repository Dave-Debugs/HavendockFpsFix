using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class QualityTweaks : FixManager.IFix
    {
        public string Name => "QualityTweaks";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private static QualityTweaks _self;
        private static ConfigEntry<int> _cascades;
        private static ConfigEntry<float> _distance;
        private static ConfigEntry<bool> _lowWater;

        private static bool _cascOwned, _distOwned, _waterOwned;
        private static int _cascOrig, _cascOurs;
        private static float _distOrig, _distOurs;
        private static GameObject _kws, _low;
        private static bool _kwsOrig, _lowOrig;
        private static long _reapplied;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "QualityTweaks", true,
                "Applies the shadow settings in the [QualityTweaks] section while the mod is on, and puts the game's own values back when it is off.");
            _cascades = cfg.Bind("QualityTweaks", "ShadowCascades", 2,
                "Sun shadow cascades: 1, 2 or 4 (the game uses 4). 2 is much cheaper and looks almost the same. 0 = keep the game's setting.");
            _distance = cfg.Bind("QualityTweaks", "ShadowDistance", 0f,
                "Shadow distance in metres. 0 = keep the game's setting.");
            _lowWater = cfg.Bind("QualityTweaks", "LowQualityWater", false,
                "true = use the game's low-quality water (the same as turning water reflections off in the game's settings).");
        }

        public void Apply(Harmony harmony)
        {
            var go = new GameObject("HavendockFpsFix.QualityTweaks") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            Applied = true;
        }

        public string Stats() => !Applied ? "not applied"
            : $"cascades {(_cascOwned ? _cascOrig + "->" + _cascOurs : "untouched")}, distance {(_distOwned ? _distOrig + "->" + _distOurs : "untouched")}, low water {(_waterOwned ? "on" : "off")}, {_reapplied} re-applies";

        public void ResetStats() { _reapplied = 0; }

        private static void Tick()
        {
            if (!FixManager.IsActive(_self)) { RestoreAll(); return; }
            ApplyCascades();
            ApplyDistance();
            ApplyWater();
        }

        private static void ApplyCascades()
        {
            int want = _cascades.Value;
            if (want != 1 && want != 2 && want != 4) { RestoreCascades(); return; }
            int cur = QualitySettings.shadowCascades;
            if (!_cascOwned)
            {
                if (cur == want) return;
                _cascOrig = cur; _cascOwned = true;
                Plugin.Log.LogInfo($"QualityTweaks: shadowCascades {cur} -> {want}");
            }
            else if (cur != _cascOurs && cur != want) { _cascOrig = cur; _reapplied++; Plugin.Log.LogInfo($"QualityTweaks: shadowCascades changed by the game to {cur}; re-applying {want}"); }
            _cascOurs = want;
            if (cur != want) QualitySettings.shadowCascades = want;
        }

        private static void ApplyDistance()
        {
            float want = _distance.Value;
            if (want <= 1f) { RestoreDistance(); return; }
            float cur = QualitySettings.shadowDistance;
            if (!_distOwned)
            {
                if (Mathf.Abs(cur - want) < 0.01f) return;
                _distOrig = cur; _distOwned = true;
                Plugin.Log.LogInfo($"QualityTweaks: shadowDistance {cur} -> {want}");
            }
            else if (Mathf.Abs(cur - _distOurs) > 0.01f && Mathf.Abs(cur - want) > 0.01f) { _distOrig = cur; _reapplied++; }
            _distOurs = want;
            if (Mathf.Abs(cur - want) > 0.01f) QualitySettings.shadowDistance = want;
        }

        private static bool ResolveWater()
        {
            if (_kws != null && _low != null) return true;
            _waterOwned = false;
            var t = AccessTools.TypeByName("ToggleWaterQuality");
            if (t == null) return false;
            var objs = UnityEngine.Object.FindObjectsByType(t, FindObjectsSortMode.None);
            if (objs == null || objs.Length == 0) return false;
            var fk = AccessTools.Field(t, "kwsWater");
            var fl = AccessTools.Field(t, "lowQualityWater");
            if (fk == null || fl == null) return false;
            _kws = fk.GetValue(objs[0]) as GameObject;
            _low = fl.GetValue(objs[0]) as GameObject;
            return _kws != null && _low != null;
        }

        private static void ApplyWater()
        {
            if (!_lowWater.Value) { RestoreWater(); return; }
            if (!ResolveWater()) return;
            if (!_waterOwned)
            {
                _kwsOrig = _kws.activeSelf; _lowOrig = _low.activeSelf; _waterOwned = true;
                Plugin.Log.LogInfo($"QualityTweaks: low-quality water on (KWS {_kwsOrig}->False, low {_lowOrig}->True)");
            }
            if (_kws.activeSelf) { _kws.SetActive(false); _reapplied++; }
            if (!_low.activeSelf) { _low.SetActive(true); _reapplied++; }
        }

        private static void RestoreCascades() { if (_cascOwned) { QualitySettings.shadowCascades = _cascOrig; _cascOwned = false; } }
        private static void RestoreDistance() { if (_distOwned) { QualitySettings.shadowDistance = _distOrig; _distOwned = false; } }
        private static void RestoreWater()
        {
            if (!_waterOwned) return;
            _waterOwned = false;
            if (_kws != null) _kws.SetActive(_kwsOrig);
            if (_low != null) _low.SetActive(_lowOrig);
        }
        private static void RestoreAll() { RestoreCascades(); RestoreDistance(); RestoreWater(); }

        private sealed class Runner : MonoBehaviour
        {
            private float _next;
            private void Update()
            {
                if (Time.unscaledTime < _next) return;
                _next = Time.unscaledTime + 1f;
                try { Tick(); }
                catch (Exception e)
                {
                    Plugin.Log.LogError("QualityTweaks error, restoring the settings and pausing: " + e);
                    try { RestoreAll(); } catch { }
                    enabled = false;
                }
            }
        }
    }
}
