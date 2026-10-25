using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class AnimatorEvents : FixManager.IFix
    {
        public string Name => "AnimatorEvents";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static AnimatorEvents _self;
        private static readonly Dictionary<Animator, RuntimeAnimatorController> Changed = new Dictionary<Animator, RuntimeAnimatorController>();
        private static readonly Dictionary<RuntimeAnimatorController, string[]> Functions = new Dictionary<RuntimeAnimatorController, string[]>();
        private static readonly Dictionary<Type, Dictionary<string, bool>> Receives = new Dictionary<Type, Dictionary<string, bool>>();
        private static readonly List<MonoBehaviour> Buf = new List<MonoBehaviour>();
        private static long _scans, _considered, _switchedOff, _restored;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "AnimatorEvents", true,
                "Animations stop sending events that no script on the object listens for (most of the game's animations have no events at all). Nothing that listens is affected.");
        }

        public void Apply(Harmony harmony)
        {
            FixManager.AddRunner<Runner>("HavendockFpsFix.AnimatorEvents");
            Applied = true;
        }

        public void Restore() { RestoreAll(); }

        public string Stats() => !Applied ? "not applied"
            : $"{Changed.Count} animators not sending unheard events ({_switchedOff} switched, {_restored} given back, {_considered} examined in {_scans} scans)";

        public void ResetStats() { _scans = _considered = _switchedOff = _restored = 0; }

        private static string[] EventFunctions(RuntimeAnimatorController ctrl)
        {
            string[] fns;
            if (Functions.TryGetValue(ctrl, out fns)) return fns;
            var set = new HashSet<string>();
            var clips = ctrl.animationClips;
            if (clips != null)
                foreach (var clip in clips)
                {
                    if (clip == null) continue;
                    foreach (var ev in clip.events)
                        if (!string.IsNullOrEmpty(ev.functionName)) set.Add(ev.functionName);
                }
            fns = new string[set.Count];
            set.CopyTo(fns);
            Functions[ctrl] = fns;
            return fns;
        }

        private static bool TypeReceives(Type type, string fn)
        {
            Dictionary<string, bool> perFn;
            if (!Receives.TryGetValue(type, out perFn)) Receives[type] = perFn = new Dictionary<string, bool>();
            bool found;
            if (perFn.TryGetValue(fn, out found)) return found;
            found = false;
            for (var t = type; t != null && t != typeof(MonoBehaviour) && !found; t = t.BaseType)
                foreach (var m in t.GetMethods(Any))
                    if (m.Name == fn) { found = true; break; }
            perFn[fn] = found;
            return found;
        }

        private static bool HasReceiver(Animator a, string[] fns)
        {
            a.GetComponents(Buf);
            try
            {
                for (int i = 0; i < Buf.Count; i++)
                {
                    var mb = Buf[i];
                    if (mb == null) continue;
                    var t = mb.GetType();
                    for (int k = 0; k < fns.Length; k++) if (TypeReceives(t, fns[k])) return true;
                }
                return false;
            }
            finally { Buf.Clear(); }
        }

        private static bool Listens(Animator a, string[] fns)
        {
            return fns.Length > 0 && HasReceiver(a, fns);
        }

        private static void Consider(Animator a)
        {
            if (a == null) return;
            _considered++;
            var ctrl = a.runtimeAnimatorController;
            RuntimeAnimatorController before;
            if (Changed.TryGetValue(a, out before))
            {
                if (ctrl == null || ctrl != before || Listens(a, EventFunctions(ctrl)))
                {
                    a.fireEvents = true;
                    Changed.Remove(a);
                    _restored++;
                }
                return;
            }
            if (ctrl == null || !a.fireEvents) return;
            if (Listens(a, EventFunctions(ctrl))) return;
            a.fireEvents = false;
            Changed[a] = ctrl;
            _switchedOff++;
        }

        private static void RestoreAll()
        {
            foreach (var kv in Changed) if (kv.Key != null) kv.Key.fireEvents = true;
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
                    Plugin.Log.LogError("AnimatorEvents error, restoring all animators and pausing: " + e);
                    try { RestoreAll(); } catch { }
                    enabled = false;
                }
            }

            private void Tick()
            {
                if (!FixManager.IsActive(_self))
                {
                    if (Changed.Count > 0) RestoreAll();
                    _queue = null;
                    return;
                }
                if (_queue == null || _qi >= _queue.Length)
                {
                    if (Time.unscaledTime < _nextScan) return;
                    _nextScan = Time.unscaledTime + 5f;
                    Functions.Clear();
                    _queue = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    _qi = 0;
                    _scans++;
                    if ((_scans & 15) == 0)
                    {
                        var dead = new List<Animator>();
                        foreach (var kv in Changed) if (kv.Key == null) dead.Add(kv.Key);
                        foreach (var d in dead) Changed.Remove(d);
                    }
                }
                int n = 0;
                while (_qi < _queue.Length && n++ < 120) Consider(_queue[_qi++]);
            }
        }
    }
}
