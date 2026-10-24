using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class WorldCanvasCulling : FixManager.IFix
    {
        public string Name => "WorldCanvasCulling";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private static WorldCanvasCulling _self;
        private static ConfigEntry<float> _marginIn, _marginOut;
        private static ConfigEntry<int> _slices, _maxEnables;
        private static readonly HashSet<Canvas> Mine = new HashSet<Canvas>();
        private static long _evals, _disables, _enables, _frames;
        private static int _lastManaged;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "WorldCanvasCulling", true,
                "Hides the floating UI above buildings while it is off screen. It comes back before it scrolls into view.");
            _marginIn = cfg.Bind("WorldCanvasCulling", "MarginIn", 0.2f,
                "How far outside the screen (as a fraction of the screen size) the floating UI is switched back on.");
            _marginOut = cfg.Bind("WorldCanvasCulling", "MarginOut", 0.35f,
                "How far outside the screen (as a fraction of the screen size) the floating UI is switched off.");
            _slices = cfg.Bind("WorldCanvasCulling", "Slices", 6,
                "Each building's floating UI is re-checked once every this many frames.");
            _maxEnables = cfg.Bind("WorldCanvasCulling", "MaxEnablesPerFrame", 40,
                "Most floating UI panels switched back on in a single frame.");
        }

        public void Apply(Harmony harmony)
        {
            var setRendering = AccessTools.Method(typeof(BuildingScript), "SetRendering", new[] { typeof(bool) });
            if (setRendering == null) throw new MissingMethodException("BuildingScript.SetRendering(bool) not found");
            if (AccessTools.Field(typeof(BuildingScript), "buildingCanvas") == null) throw new MissingFieldException("BuildingScript.buildingCanvas not found");
            harmony.Patch(setRendering, prefix: new HarmonyMethod(typeof(WorldCanvasCulling), nameof(ReleaseOwnership)));
            var go = new GameObject("HavendockFpsFix.WorldCanvasCulling") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            Applied = true;
        }

        internal static void ReleaseOwnership(BuildingScript __instance)
        {
            try { var c = __instance.buildingCanvas; if (c != null) Mine.Remove(c); } catch { }
        }

        public string Stats()
        {
            if (!Applied) return "not applied";
            return $"{Mine.Count} canvases off now ({_lastManaged} buildings managed), {_disables} disables, {_enables} enables, {_evals} checks over {_frames} frames";
        }

        public void ResetStats() { _evals = _disables = _enables = _frames = 0; }

        private static void RestoreAll()
        {
            foreach (var c in Mine) if (c != null) c.enabled = true;
            Mine.Clear();
        }

        private sealed class Runner : MonoBehaviour
        {
            private struct Cam { public Matrix4x4 vp; public int mask; }
            private readonly List<Cam> _cams = new List<Cam>();
            private int _cursor;
            private bool _wasActive;

            private void Update()
            {
                try { Tick(); }
                catch (Exception e)
                {
                    Plugin.Log.LogError("WorldCanvasCulling error, restoring all canvases and pausing: " + e);
                    try { RestoreAll(); } catch { }
                    enabled = false;
                }
            }

            private void Tick()
            {
                bool active = FixManager.IsActive(_self);
                if (!active)
                {
                    if (_wasActive || Mine.Count > 0) RestoreAll();
                    _wasActive = false;
                    return;
                }
                _wasActive = true;
                _frames++;

                var mgr = BuildingManager.instance;
                if (mgr == null || mgr.buildings == null) return;
                if (SaveLoad.settings != null && SaveLoad.settings.HideBuildings) return;

                _cams.Clear();
                foreach (var cam in Camera.allCameras)
                {
                    if (cam == null || !cam.enabled || cam.targetTexture != null) continue;
                    _cams.Add(new Cam { vp = cam.projectionMatrix * cam.worldToCameraMatrix, mask = cam.cullingMask });
                }
                if (_cams.Count == 0) { if (Mine.Count > 0) RestoreAll(); return; }

                var b = mgr.buildings; var d = mgr.decks;
                int nb = b.Count, nd = d != null ? d.Count : 0, total = nb + nd;
                _lastManaged = total;
                if (total == 0) return;
                int perFrame = Math.Max(32, (total + _slices.Value - 1) / Math.Max(1, _slices.Value));
                float mIn = 1f + 2f * _marginIn.Value, mOut = 1f + 2f * Math.Max(_marginIn.Value, _marginOut.Value);
                int enablesLeft = _maxEnables.Value;

                for (int k = 0; k < perFrame && k < total; k++)
                {
                    if (_cursor >= total) _cursor = 0;
                    int idx = _cursor++;
                    BuildingScript bs = idx < nb ? b[idx] : (d[idx - nb] != null ? d[idx - nb].buildingScript : null);
                    if (bs == null) continue;
                    var c = bs.buildingCanvas;
                    if (c == null) continue;
                    _evals++;
                    bool mine = Mine.Contains(c);
                    if (!mine && !c.enabled) continue;
                    Vector3 p = c.transform.position;
                    int layerBit = 1 << c.gameObject.layer;
                    if (!mine)
                    {
                        if (!SeenByAny(p, layerBit, mOut)) { c.enabled = false; Mine.Add(c); _disables++; }
                    }
                    else if (enablesLeft > 0 && SeenByAny(p, layerBit, mIn))
                    {
                        c.enabled = true; Mine.Remove(c); _enables++; enablesLeft--;
                    }
                }
                if ((_frames & 255) == 0) Mine.RemoveWhere(c => c == null);
            }

            private bool SeenByAny(Vector3 p, int layerBit, float ndcLimit)
            {
                for (int i = 0; i < _cams.Count; i++)
                {
                    var cam = _cams[i];
                    if ((cam.mask & layerBit) == 0) continue;
                    var m = cam.vp;
                    float w = m.m30 * p.x + m.m31 * p.y + m.m32 * p.z + m.m33;
                    if (w <= 0f) continue;
                    float x = (m.m00 * p.x + m.m01 * p.y + m.m02 * p.z + m.m03) / w;
                    float y = (m.m10 * p.x + m.m11 * p.y + m.m12 * p.z + m.m13) / w;
                    if (x >= -ndcLimit && x <= ndcLimit && y >= -ndcLimit && y <= ndcLimit) return true;
                }
                return false;
            }
        }
    }
}
