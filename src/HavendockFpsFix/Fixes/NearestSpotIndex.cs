using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class NearestSpotIndex : FixManager.IFix
    {
        public string Name => "NearestSpotIndex";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private static NearestSpotIndex _self;
        private static ConfigEntry<bool> _verify;

        private static List<BuildingScript> _source;
        private static int _sourceVersion = int.MinValue, _sourceCount = -1;
        private static bool _sourceUnsafe;
        private static readonly Dictionary<BuildingType, List<BuildingScript>> _byType = new Dictionary<BuildingType, List<BuildingScript>>();
        private static readonly List<BuildingScript> _empty = new List<BuildingScript>();
        private static AccessTools.FieldRef<List<BuildingScript>, int> _versionRef;

        private static bool _forceFull, _inVerify;
        private static MethodInfo _target;

        private static long _calls, _scanned, _fullSize, _rebuilds, _verified, _mismatches, _fallbacks;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "NearestSpotIndex", true,
                "Settlers looking for the nearest bed, food, drink and so on no longer check every building one by one. Same results, much less work.");
            _verify = cfg.Bind("Fixes", "NearestSpotVerify", false,
                "Troubleshooting only: double-checks every answer against the game's original method (slower). Leave this false.");
        }

        public void Apply(Harmony harmony)
        {
            _target = AccessTools.Method(typeof(BuildingManager), "getNearestSpot",
                new[] { typeof(BuildingType), typeof(Character), typeof(int), typeof(bool), typeof(ItemType) });
            if (_target == null) throw new MissingMethodException("BuildingManager.getNearestSpot(BuildingType, Character, int, bool, ItemType) not found");
            try { _versionRef = AccessTools.FieldRefAccess<List<BuildingScript>, int>("_version"); }
            catch { _versionRef = null; Plugin.Log.LogWarning("NearestSpotIndex: List._version not found; index will rebuild every frame."); }

            harmony.Patch(_target,
                transpiler: new HarmonyMethod(typeof(NearestSpotIndex), nameof(Transpiler)),
                postfix: new HarmonyMethod(typeof(NearestSpotIndex), nameof(VerifyPostfix)));
            Applied = _transpiled;
            if (!Applied) Plugin.Log.LogWarning("NearestSpotIndex: IL pattern not found (game updated?). Fix inactive; vanilla behaviour.");
        }

        private static bool _transpiled;

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getter = AccessTools.PropertyGetter(typeof(BuildingManager), "buildings");
            var helper = AccessTools.Method(typeof(NearestSpotIndex), nameof(Buildings));
            var codes = new List<CodeInstruction>(instructions);
            int hits = 0, at = -1;
            for (int i = 1; i < codes.Count; i++)
                if ((codes[i].opcode == OpCodes.Call || codes[i].opcode == OpCodes.Callvirt) && Equals(codes[i].operand, getter) && codes[i - 1].IsLdarg(0))
                { hits++; at = i; }
            if (hits != 1)
            {
                Plugin.Log.LogWarning($"NearestSpotIndex: expected 1 'this.buildings' load in getNearestSpot, found {hits}; leaving method unchanged.");
                _transpiled = false;
                return codes;
            }
            var ldType = new CodeInstruction(OpCodes.Ldarg_1);
            ldType.labels.AddRange(codes[at].labels); codes[at].labels.Clear();
            ldType.blocks.AddRange(codes[at].blocks); codes[at].blocks.Clear();
            codes[at] = new CodeInstruction(OpCodes.Call, helper);
            codes.Insert(at, ldType);
            _transpiled = true;
            return codes;
        }

        public static List<BuildingScript> Buildings(BuildingManager mgr, BuildingType type)
        {
            var all = mgr.buildings;
            if (_forceFull || _self == null || !FixManager.IsActive(_self) || all == null) return all;
            _calls++;
            _fullSize += all.Count;
            EnsureIndex(all);
            if (_sourceUnsafe) { _fallbacks++; _scanned += all.Count; return all; }
            List<BuildingScript> list;
            if (!_byType.TryGetValue(type, out list)) list = _empty;
            _scanned += list.Count;
            return list;
        }

        private static int _lastFrame = -1;

        private static void EnsureIndex(List<BuildingScript> all)
        {
            int version = _versionRef != null ? _versionRef(all) : 0;
            bool frameRule = _versionRef == null && Time.frameCount != _lastFrame;
            if (ReferenceEquals(all, _source) && version == _sourceVersion && all.Count == _sourceCount && !frameRule) return;

            _lastFrame = Time.frameCount;
            _rebuilds++;
            foreach (var l in _byType.Values) l.Clear();
            _sourceUnsafe = false;
            for (int i = 0; i < all.Count; i++)
            {
                var b = all[i];
                if (ReferenceEquals(b, null) || ReferenceEquals(b.data, null)) { _sourceUnsafe = true; break; }
                var t = b.data.buildingType;
                List<BuildingScript> list;
                if (!_byType.TryGetValue(t, out list)) _byType[t] = list = new List<BuildingScript>();
                list.Add(b);
            }
            _source = all;
            _sourceVersion = version;
            _sourceCount = all.Count;
        }

        internal static void VerifyPostfix(BuildingManager __instance, BuildingType buildingType, Character character, int minimumProducts,
            bool requiresFreeStandHereSpot, ItemType mealRequirement, ref BuildingScript __result)
        {
            if (_inVerify || _self == null || !_verify.Value || !FixManager.IsActive(_self)) return;
            int idxFast = character != null && character.agentAi != null ? character.agentAi.resourceIndexToTakeFood : 0;
            BuildingScript vanilla;
            _inVerify = true; _forceFull = true;
            try
            {
                vanilla = (BuildingScript)_target.Invoke(__instance, new object[] { buildingType, character, minimumProducts, requiresFreeStandHereSpot, mealRequirement });
            }
            catch (TargetInvocationException e) { Plugin.Log.LogWarning("NearestSpotVerify: vanilla call threw " + e.InnerException?.GetType().Name); return; }
            finally { _inVerify = false; _forceFull = false; }
            int idxVanilla = character != null && character.agentAi != null ? character.agentAi.resourceIndexToTakeFood : 0;
            _verified++;
            if (!ReferenceEquals(vanilla, __result) || idxFast != idxVanilla)
            {
                _mismatches++;
                if (_mismatches <= 20)
                    Plugin.Log.LogError($"NearestSpotVerify MISMATCH #{_mismatches}: type={buildingType} char={(character ? character.name : "null")} fast={(__result ? __result.name : "null")}/{idxFast} vanilla={(vanilla ? vanilla.name : "null")}/{idxVanilla}");
                __result = vanilla;
            }
        }

        public string Stats()
        {
            if (!Applied) return "not applied";
            double saved = _fullSize > 0 ? 100.0 * (1.0 - (double)_scanned / _fullSize) : 0;
            string s = $"{_calls} calls, scanned {_scanned} of {_fullSize} buildings ({saved:F1}% skipped), {_rebuilds} index rebuilds, {_fallbacks} fallbacks";
            if (_verified > 0 || _verify.Value) s += $", verified {_verified}, mismatches {_mismatches}";
            return s;
        }

        public void ResetStats() { _calls = _scanned = _fullSize = _rebuilds = _verified = _fallbacks = 0; }
    }
}
