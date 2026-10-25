using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class PostEffects : FixManager.IFix
    {
        public string Name => "PostEffects";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private static PostEffects _self;
        private static ConfigEntry<string> _off;
        private static Type _volumeType;
        private static FieldInfo _shared, _internal, _settings;
        private static readonly List<object> Owned = new List<object>();
        private static readonly HashSet<string> Wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string _wantedText = "", _lastSeen = "";

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "PostEffects", true,
                "Lets you switch off chosen post-processing effects with the setting below. Does nothing while that setting is empty.");
            _off = cfg.Bind("PostEffects", "Off", "",
                "Post-processing effects to switch off, separated by commas, e.g. AmbientOcclusion, Bloom. Empty = keep them all. The names in use are written to the log when a world loads. Changes the look.");
        }

        public void Apply(Harmony harmony)
        {
            _volumeType = AccessTools.TypeByName("UnityEngine.Rendering.PostProcessing.PostProcessVolume");
            if (_volumeType == null) throw new TypeLoadException("this game build has no post-processing volumes");
            _shared = AccessTools.Field(_volumeType, "sharedProfile");
            _internal = AccessTools.Field(_volumeType, "m_InternalProfile");
            var profileType = AccessTools.TypeByName("UnityEngine.Rendering.PostProcessing.PostProcessProfile");
            _settings = profileType != null ? AccessTools.Field(profileType, "settings") : null;
            if (_shared == null || _settings == null) throw new MissingFieldException("post-processing profile fields not found");
            FixManager.AddRunner<Runner>("HavendockFpsFix.PostEffects");
            Applied = true;
        }

        public void Restore() { RestoreAll(); }

        public string Stats() => Applied ? $"{Owned.Count} effects switched off" : "not applied";
        public void ResetStats() { }

        private static FieldInfo ActiveField(object s) { return AccessTools.Field(s.GetType(), "active"); }

        private static IEnumerable<IList> Lists()
        {
            foreach (var v in UnityEngine.Object.FindObjectsByType(_volumeType, FindObjectsSortMode.None))
            {
                var b = v as Behaviour;
                if (b == null || !b.isActiveAndEnabled) continue;
                foreach (var f in new[] { _internal, _shared })
                {
                    if (f == null) continue;
                    var prof = f.GetValue(v);
                    if (prof == null) continue;
                    var list = _settings.GetValue(prof) as IList;
                    if (list != null) yield return list;
                }
            }
        }

        private static void Tick()
        {
            string text = _off.Value ?? "";
            if (text != _wantedText)
            {
                _wantedText = text;
                Wanted.Clear();
                foreach (var part in text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)) Wanted.Add(part.Trim());
                RestoreAll();
            }

            var seen = new List<string>();
            foreach (var list in Lists())
                foreach (var s in list)
                {
                    if (s == null) continue;
                    string name = s.GetType().Name;
                    if (!seen.Contains(name)) seen.Add(name);
                    if (!FixManager.IsActive(_self) || !Wanted.Contains(name) || Owned.Contains(s)) continue;
                    var active = ActiveField(s);
                    if (active == null || !(bool)active.GetValue(s)) continue;
                    active.SetValue(s, false);
                    Owned.Add(s);
                }
            seen.Sort(StringComparer.Ordinal);
            string seenText = string.Join(", ", seen.ToArray());
            if (seenText.Length > 0 && seenText != _lastSeen)
            {
                _lastSeen = seenText;
                Plugin.Log.LogInfo("Post-processing effects in this scene: " + seenText);
            }
            if (!FixManager.IsActive(_self)) RestoreAll();
        }

        private static void RestoreAll()
        {
            foreach (var s in Owned)
            {
                if (s == null) continue;
                var active = ActiveField(s);
                if (active != null) active.SetValue(s, true);
            }
            Owned.Clear();
        }

        private sealed class Runner : MonoBehaviour
        {
            private float _next;

            private void Update()
            {
                if (Time.unscaledTime < _next) return;
                _next = Time.unscaledTime + 2f;
                try { Tick(); }
                catch (Exception e)
                {
                    Plugin.Log.LogError("PostEffects error, restoring and pausing: " + e);
                    try { RestoreAll(); } catch { }
                    enabled = false;
                }
            }
        }
    }
}
