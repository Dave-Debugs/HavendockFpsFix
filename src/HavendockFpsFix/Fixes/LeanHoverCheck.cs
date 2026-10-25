using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HavendockFpsFix
{
    internal sealed class LeanHoverCheck : FixManager.IFix
    {
        public string Name => "LeanHoverCheck";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private static LeanHoverCheck _self;
        private static AccessTools.FieldRef<SelectObjectScript, LayerMask> _uiMask;
        private static AccessTools.FieldRef<SelectObjectScript, bool> _overUi;
        private static readonly List<RaycastResult> Results = new List<RaycastResult>();
        private static PointerEventData _pointer;
        private static EventSystem _pointerSystem;
        private static long _calls;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "LeanHoverCheck", true,
                "The mouse-over-UI check that runs every frame reuses its working lists instead of making new ones, so there is less garbage to clean up. Same answers.");
        }

        public void Apply(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(SelectObjectScript), "IsPointerOverUIObject", new[] { typeof(Vector2) });
            if (target == null || target.ReturnType != typeof(bool)) throw new MissingMethodException("SelectObjectScript.IsPointerOverUIObject(Vector2) not found");
            _uiMask = AccessTools.FieldRefAccess<SelectObjectScript, LayerMask>("whatIsUI");
            _overUi = AccessTools.FieldRefAccess<SelectObjectScript, bool>("isOverUI");
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(LeanHoverCheck), nameof(Prefix)));
            Applied = true;
        }

        internal static bool Prefix(SelectObjectScript __instance, Vector2 __0, ref bool __result)
        {
            if (_self == null || !FixManager.IsActive(_self)) return true;
            var system = EventSystem.current;
            if (system == null) return true;
            if (_pointer == null || _pointerSystem != system)
            {
                _pointer = new PointerEventData(system);
                _pointerSystem = system;
            }
            _pointer.position = new Vector2(__0.x, __0.y);
            bool over = false;
            try
            {
                system.RaycastAll(_pointer, Results);
                int mask = _uiMask(__instance).value;
                for (int i = 0; i < Results.Count; i++)
                    if ((mask & (1 << Results[i].gameObject.layer)) != 0) { over = true; break; }
            }
            finally { Results.Clear(); }
            _calls++;
            _overUi(__instance) = over;
            __result = over;
            return false;
        }

        public void Restore() { _pointer = null; _pointerSystem = null; }

        public string Stats() => Applied ? $"{_calls} checks without new garbage" : "not applied";
        public void ResetStats() { _calls = 0; }
    }
}
