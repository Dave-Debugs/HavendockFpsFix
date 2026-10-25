using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace HavendockFpsFix
{
    internal sealed class AnimatorCulling : FixManager.IFix
    {
        public string Name => "AnimatorCulling";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private static AnimatorCulling _self;
        private static ConfigEntry<float> _scanSeconds;
        private static ConfigEntry<int> _perFrame;
        private static ConfigEntry<bool> _includeSettlers;
        private static readonly Dictionary<Animator, AnimatorCullingMode> Changed = new Dictionary<Animator, AnimatorCullingMode>();
        private static readonly List<MonoBehaviour> Buf = new List<MonoBehaviour>();
        private static readonly HashSet<string> Protected = new HashSet<string> { "Sun", "Crane", "ItemPanel" };
        private static long _scans, _considered, _changed, _skipMode, _skipRoot, _skipNav, _skipNoRenderer, _skipUi, _skipProtected;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "AnimatorCulling", true,
                "Pauses decorative animations while they are off screen. They carry on as soon as they are visible again.");
            _scanSeconds = cfg.Bind("AnimatorCulling", "ScanSeconds", 5f,
                "Seconds between scans for animated objects.");
            _perFrame = cfg.Bind("AnimatorCulling", "PerFrame", 150,
                "Animated objects checked per frame during a scan.");
            _includeSettlers = cfg.Bind("AnimatorCulling", "IncludeAgents", true,
                "Also pause the animation of off-screen settlers and animals that walk using the game's pathfinding. Their movement is not affected. Set to false if anything looks frozen when you scroll back to it.");
        }

        public void Apply(Harmony harmony)
        {
            FixManager.AddRunner<Runner>("HavendockFpsFix.AnimatorCulling");
            Applied = true;
        }

        public string Stats() => !Applied ? "not applied"
            : $"{Changed.Count} animators set to CullCompletely ({_changed} changes, {_considered} examined in {_scans} scans); skipped: mode {_skipMode}, root motion {_skipRoot}, NavMeshAgent {_skipNav}, no renderer {_skipNoRenderer}, UI {_skipUi}, protected {_skipProtected}";

        public void Restore() { RestoreAll(); }

        public void ResetStats() { _scans = _considered = _changed = _skipMode = _skipRoot = _skipNav = _skipNoRenderer = _skipUi = _skipProtected = 0; }

        private static void Consider(Animator a)
        {
            if (a == null) return;
            _considered++;
            if (Changed.ContainsKey(a)) return;
            if (a.cullingMode != AnimatorCullingMode.CullUpdateTransforms) { _skipMode++; return; }
            if (a.applyRootMotion) { _skipRoot++; return; }
            var nav = a.GetComponentInParent<NavMeshAgent>(true);
            if (nav != null && (!_includeSettlers.Value || !nav.updatePosition)) { _skipNav++; return; }
            if (a.GetComponentInChildren<Renderer>(true) == null) { _skipNoRenderer++; return; }
            if (a.GetComponentInParent<Canvas>(true) != null) { _skipUi++; return; }
            a.GetComponentsInParent(true, Buf);
            bool prot = false;
            for (int i = 0; i < Buf.Count; i++) { var m = Buf[i]; if (m != null && Protected.Contains(m.GetType().Name)) { prot = true; break; } }
            Buf.Clear();
            if (prot) { _skipProtected++; return; }
            Changed[a] = a.cullingMode;
            a.cullingMode = AnimatorCullingMode.CullCompletely;
            _changed++;
        }

        private static void RestoreAll()
        {
            foreach (var kv in Changed) if (kv.Key != null) kv.Key.cullingMode = kv.Value;
            Changed.Clear();
        }

        private sealed class Runner : MonoBehaviour
        {
            private float _nextScan;
            private Animator[] _queue;
            private int _qi;

            private void Update()
            {
                try { Tick(); }
                catch (Exception e)
                {
                    Plugin.Log.LogError("AnimatorCulling error, restoring all animators and pausing: " + e);
                    try { RestoreAll(); } catch { }
                    enabled = false;
                }
            }

            private void Tick()
            {
                if (!FixManager.IsActive(_self))
                {
                    if (Changed.Count > 0) { RestoreAll(); Plugin.Log.LogInfo("AnimatorCulling: restored."); }
                    _queue = null;
                    return;
                }
                if (_queue == null || _qi >= _queue.Length)
                {
                    if (Time.unscaledTime < _nextScan) return;
                    _nextScan = Time.unscaledTime + Math.Max(2f, _scanSeconds.Value);
                    _queue = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    _qi = 0; _scans++;
                }
                int n = 0, max = Math.Max(20, _perFrame.Value);
                while (_qi < _queue.Length && n++ < max) Consider(_queue[_qi++]);
                if (_scans == 1 && _qi >= _queue.Length && _changed > 0 && _considered == _queue.Length)
                    Plugin.Log.LogInfo("AnimatorCulling first pass: " + _self.Stats());
            }
        }
    }
}
