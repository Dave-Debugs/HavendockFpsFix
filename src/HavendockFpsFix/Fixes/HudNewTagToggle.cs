using System;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class HudNewTagToggle : FixManager.IFix
    {
        public string Name => "HudNewTagToggle";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private static HudNewTagToggle _self;
        private static AccessTools.FieldRef<BaseButton, BuildingType[]> _types;
        private static long _calls, _toggles;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "HudNewTagToggle", true,
                "Stops the toolbar's NEW tags from rebuilding the whole HUD every frame. Side effect: the NEW tag's small wiggle animation now plays.");
        }

        public void Apply(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(BaseButton), "Update");
            if (target == null) throw new MissingMethodException("BaseButton.Update not found");
            _types = AccessTools.FieldRefAccess<BaseButton, BuildingType[]>("buildingTypes");
            if (AccessTools.Method(typeof(BuildingButton), "CheckIfShouldShowNewTag", new[] { typeof(BuildingType) }) == null)
                throw new MissingMethodException("BuildingButton.CheckIfShouldShowNewTag not found");
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(HudNewTagToggle), nameof(Prefix)));
            Applied = true;
        }

        internal static bool Prefix(BaseButton __instance)
        {
            if (_self == null || !FixManager.IsActive(_self)) return true;
            _calls++;
            if (__instance.index == 0)
                _types(__instance) = BuildingManager.instance.buildingButtons.GetButtonOrder(__instance.index);
            bool want = false;
            if (!__instance.isLocked)
            {
                var arr = _types(__instance);
                for (int i = 0; i < arr.Length; i++)
                    if (BuildingButton.CheckIfShouldShowNewTag(arr[i])) want = true;
            }
            var tag = __instance.newTag;
            if (tag.activeSelf != want) { tag.SetActive(want); _toggles++; }
            return false;
        }

        public string Stats() => Applied ? $"{_calls} updates, {_toggles} actual tag changes" : "not applied";
        public void Restore() { }

        public void ResetStats() { _calls = _toggles = 0; }
    }
}
