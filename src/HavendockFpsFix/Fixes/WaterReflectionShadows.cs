using System;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class WaterReflectionShadows : FixManager.IFix
    {
        public string Name => "WaterReflectionShadows";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private static WaterReflectionShadows _self;
        private static long _passes;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "WaterReflectionShadows", true,
                "No shadows inside the water's reflection. The water keeps its reflections and the rest of the world keeps its shadows.");
        }

        public void Apply(Harmony harmony)
        {
            var water = AccessTools.TypeByName("UnityStandardAssets.Water.Water");
            var target = water != null ? AccessTools.Method(water, "OnWillRenderObject") : null;
            if (target == null) throw new MissingMethodException("this game build has no reflective water script");
            harmony.Patch(target,
                prefix: new HarmonyMethod(typeof(WaterReflectionShadows), nameof(Prefix)),
                finalizer: new HarmonyMethod(typeof(WaterReflectionShadows), nameof(Finalizer)));
            Applied = true;
        }

        internal static void Prefix(out int __state)
        {
            __state = -1;
            if (_self == null || !FixManager.IsActive(_self)) return;
            var current = QualitySettings.shadows;
            if (current == ShadowQuality.Disable) return;
            __state = (int)current;
            QualitySettings.shadows = ShadowQuality.Disable;
            _passes++;
        }

        internal static void Finalizer(int __state)
        {
            if (__state >= 0) QualitySettings.shadows = (ShadowQuality)__state;
        }

        public void Restore() { }

        public string Stats() => Applied ? $"{_passes} water passes without shadows" : "not applied";
        public void ResetStats() { _passes = 0; }
    }
}
