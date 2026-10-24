using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace HavendockFpsFix
{
    internal sealed class EmptyCanvasDormant : FixManager.IFix
    {
        public string Name => "EmptyCanvasDormant";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private sealed class Entry
        {
            public Canvas Canvas;
            public GraphicRaycaster Raycaster;
            public bool RaycasterWasOn;
            public bool Dormant;
            public int EmptyStreak;
            public FaceTowards Face;
        }

        private static EmptyCanvasDormant _self;
        private static ConfigEntry<float> _scanSeconds;
        private static ConfigEntry<int> _slices, _minEmptyChecks;
        private static ConfigEntry<bool> _billboards;
        private static readonly Dictionary<Canvas, Entry> Map = new Dictionary<Canvas, Entry>();
        private static readonly List<Entry> Known = new List<Entry>();
        private static readonly List<Entry> PendingWake = new List<Entry>();
        private static readonly List<Graphic> GraphicBuf = new List<Graphic>();
        private static readonly List<Canvas> CanvasBuf = new List<Canvas>();
        private static int _dormantCount;
        private static long _faceOff, _evals, _sleeps, _wakesByHook, _wakesByPoll, _released, _scans, _frames, _hookCalls;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "EmptyCanvasDormant", true,
                "Puts floating UI panels that currently show nothing to sleep. They wake up the moment they have something to show.");
            _scanSeconds = cfg.Bind("EmptyCanvasDormant", "ScanSeconds", 4f,
                "Seconds between scans for new floating UI panels.");
            _slices = cfg.Bind("EmptyCanvasDormant", "Slices", 30,
                "Each panel is re-checked once every this many frames.");
            _minEmptyChecks = cfg.Bind("EmptyCanvasDormant", "MinEmptyChecks", 2,
                "How many checks in a row a panel has to be empty before it sleeps.");
            _billboards = cfg.Bind("EmptyCanvasDormant", "DisableBillboards", true,
                "Also stop sleeping panels from turning to face the camera.");
        }

        public void Apply(Harmony harmony)
        {
            var onEnable = AccessTools.Method(typeof(Graphic), "OnEnable");
            if (onEnable == null) throw new MissingMethodException("UnityEngine.UI.Graphic.OnEnable not found");
            var setRendering = AccessTools.Method(typeof(BuildingScript), "SetRendering", new[] { typeof(bool) });
            if (setRendering == null) throw new MissingMethodException("BuildingScript.SetRendering(bool) not found");
            if (AccessTools.Field(typeof(BuildingScript), "buildingCanvas") == null) throw new MissingFieldException("BuildingScript.buildingCanvas not found");
            harmony.Patch(onEnable, postfix: new HarmonyMethod(typeof(EmptyCanvasDormant), nameof(GraphicOnEnable)));
            harmony.Patch(setRendering, prefix: new HarmonyMethod(typeof(EmptyCanvasDormant), nameof(BeforeSetRendering)));
            var go = new GameObject("HavendockFpsFix.EmptyCanvasDormant") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            Applied = true;
        }

        internal static void GraphicOnEnable(Graphic __instance)
        {
            if (_dormantCount == 0) return;
            try
            {
                _hookCalls++;
                __instance.GetComponentsInParent(true, CanvasBuf);
                if (CanvasBuf.Count == 0) return;
                Canvas root = CanvasBuf[CanvasBuf.Count - 1];
                CanvasBuf.Clear();
                Entry e;
                if (root != null && Map.TryGetValue(root, out e) && e.Dormant) Wake(e, "hook:" + __instance.GetType().Name);
            }
            catch (Exception ex) { CanvasBuf.Clear(); Plugin.Log.LogWarning("EmptyCanvasDormant hook: " + ex.Message); }
        }

        internal static void BeforeSetRendering(BuildingScript __instance)
        {
            try
            {
                var c = __instance.buildingCanvas;
                Entry e;
                if (c != null && Map.TryGetValue(c, out e) && e.Dormant) { Release(e); _released++; }
            }
            catch { }
        }

        private static void Sleep(Entry e)
        {
            var c = e.Canvas;
            e.Raycaster = c.GetComponent<GraphicRaycaster>();
            e.RaycasterWasOn = e.Raycaster != null && e.Raycaster.enabled;
            c.enabled = false;
            if (e.RaycasterWasOn) e.Raycaster.enabled = false;
            if (_billboards.Value)
            {
                var f = c.GetComponent<FaceTowards>();
                if (f != null && f.enabled && f.alwaysUpdate && f.angleThreshold < 1f && !f.useDegreesForYAxisInstead) { f.enabled = false; e.Face = f; _faceOff++; }
            }
            e.Dormant = true;
            _dormantCount++;
            _sleeps++;
        }

        private static void Wake(Entry e, string why)
        {
            try
            {
                RestoreFace(e);
                if (e.Canvas != null) e.Canvas.enabled = true;
                if (e.RaycasterWasOn && e.Raycaster != null) e.Raycaster.enabled = true;
                e.Dormant = false; e.EmptyStreak = 0; _dormantCount--;
                if (why.StartsWith("hook")) _wakesByHook++; else _wakesByPoll++;
            }
            catch (Exception)
            {
                if (!PendingWake.Contains(e)) PendingWake.Add(e);
            }
        }

        private static void RestoreFace(Entry e)
        {
            if (e.Face != null && !e.Face.enabled) e.Face.enabled = true;
            e.Face = null;
        }

        private static void Release(Entry e)
        {
            RestoreFace(e);
            if (e.RaycasterWasOn && e.Raycaster != null) e.Raycaster.enabled = true;
            e.Dormant = false; e.EmptyStreak = 0; _dormantCount--;
        }

        private static void WakeAll()
        {
            for (int i = 0; i < Known.Count; i++)
            {
                var e = Known[i];
                if (!e.Dormant) continue;
                RestoreFace(e);
                if (e.Canvas != null) { e.Canvas.enabled = true; if (e.RaycasterWasOn && e.Raycaster != null) e.Raycaster.enabled = true; }
                e.Dormant = false; e.EmptyStreak = 0;
            }
            _dormantCount = 0;
            PendingWake.Clear();
        }

        private static bool HasEnabledGraphic(Canvas c)
        {
            c.GetComponentsInChildren(false, GraphicBuf);
            bool any = false;
            for (int i = 0; i < GraphicBuf.Count; i++) { var g = GraphicBuf[i]; if (g != null && g.enabled) { any = true; break; } }
            GraphicBuf.Clear();
            return any;
        }

        public string Stats()
        {
            if (!Applied) return "not applied";
            return $"{_dormantCount} asleep of {Known.Count} known ({_faceOff} billboards parked), {_sleeps} sleeps, {_wakesByHook} wakes by hook, {_wakesByPoll} by poll, {_released} released to the game, {_evals} checks, {_scans} scans over {_frames} frames";
        }

        public void ResetStats() { _faceOff = _evals = _sleeps = _wakesByHook = _wakesByPoll = _released = _scans = _frames = _hookCalls = 0; }

        private sealed class Runner : MonoBehaviour
        {
            private int _cursor;
            private float _nextScan;
            private bool _wasActive;

            private void Update()
            {
                try { Tick(); }
                catch (Exception e)
                {
                    Plugin.Log.LogError("EmptyCanvasDormant error, waking all canvases and pausing: " + e);
                    try { WakeAll(); } catch { }
                    enabled = false;
                }
            }

            private void Tick()
            {
                bool active = FixManager.IsActive(_self);
                if (!active)
                {
                    if (_wasActive || _dormantCount > 0) WakeAll();
                    _wasActive = false;
                    return;
                }
                _wasActive = true;
                _frames++;

                for (int i = PendingWake.Count - 1; i >= 0; i--)
                {
                    var p = PendingWake[i]; PendingWake.RemoveAt(i);
                    if (p.Dormant) Wake(p, "retry");
                }

                if (Time.unscaledTime >= _nextScan) { _nextScan = Time.unscaledTime + Math.Max(1f, _scanSeconds.Value); Scan(); }

                int total = Known.Count;
                if (total == 0) return;
                int perFrame = Math.Max(16, (total + _slices.Value - 1) / Math.Max(1, _slices.Value));
                int need = Math.Max(1, _minEmptyChecks.Value);
                for (int k = 0; k < perFrame && k < total; k++)
                {
                    if (_cursor >= Known.Count) _cursor = 0;
                    var e = Known[_cursor];
                    if (e.Canvas == null)
                    {
                        if (e.Dormant) _dormantCount--;
                        int last = Known.Count - 1; Known[_cursor] = Known[last]; Known.RemoveAt(last);
                        continue;
                    }
                    _cursor++;
                    _evals++;
                    var c = e.Canvas;
                    if (e.Dormant)
                    {
                        if (c.enabled) { Release(e); _released++; }
                        else if (HasEnabledGraphic(c)) Wake(e, "poll");
                    }
                    else
                    {
                        if (!c.enabled || !c.gameObject.activeInHierarchy) { e.EmptyStreak = 0; continue; }
                        if (HasEnabledGraphic(c)) e.EmptyStreak = 0;
                        else if (++e.EmptyStreak >= need) Sleep(e);
                    }
                }
            }

            private static void Scan()
            {
                _scans++;
                if ((_scans & 15) == 0)
                {
                    var dead = new List<Canvas>();
                    foreach (var kv in Map) if (kv.Key == null) dead.Add(kv.Key);
                    foreach (var d in dead) Map.Remove(d);
                }
                var all = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    var c = all[i];
                    if (c == null || !c.isRootCanvas || c.renderMode != RenderMode.WorldSpace) continue;
                    if (Map.ContainsKey(c)) continue;
                    var e = new Entry { Canvas = c };
                    Map[c] = e; Known.Add(e);
                }
            }
        }
    }
}
