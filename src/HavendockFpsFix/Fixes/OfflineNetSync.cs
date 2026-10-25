using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class OfflineNetSync : FixManager.IFix
    {
        public string Name => "OfflineNetSync";
        public ConfigEntry<bool> Enabled { get; private set; }
        public bool Applied { get; private set; }

        private static OfflineNetSync _self;
        private static Type _smoothType, _viewType;
        private static Func<bool> _offline;
        private static MethodInfo _isMine;
        private static readonly List<Behaviour> Disabled = new List<Behaviour>();
        private static long _switchedOff, _restored;

        public void Bind(ConfigFile cfg)
        {
            _self = this;
            Enabled = cfg.Bind("Fixes", "OfflineNetSync", true,
                "Single player only: pauses the network smoothing on settlers, birds and boats, which does nothing without other players. It switches back on the moment the game goes online.");
        }

        public void Apply(Harmony harmony)
        {
            _smoothType = AccessTools.TypeByName("Smooth.SmoothSyncPUN2");
            var network = AccessTools.TypeByName("Photon.Pun.PhotonNetwork");
            var offline = network != null ? AccessTools.PropertyGetter(network, "OfflineMode") : null;
            if (_smoothType == null || !typeof(Behaviour).IsAssignableFrom(_smoothType) || offline == null)
                throw new TypeLoadException("this game build has no Photon network smoothing");
            _offline = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), offline);
            _viewType = AccessTools.TypeByName("Photon.Pun.PhotonView");
            _isMine = _viewType != null ? AccessTools.PropertyGetter(_viewType, "IsMine") : null;
            FixManager.AddRunner<Runner>("HavendockFpsFix.OfflineNetSync");
            Applied = true;
        }

        public void Restore() { RestoreAll(); }

        public string Stats() => !Applied ? "not applied"
            : $"{Disabled.Count} smoothing components paused now ({_switchedOff} paused in total, {_restored} given back)";

        public void ResetStats() { _switchedOff = _restored = 0; }

        private static bool Mine(Behaviour b)
        {
            if (_viewType == null || _isMine == null) return true;
            var view = b.GetComponent(_viewType);
            if (view == null) return true;
            return (bool)_isMine.Invoke(view, null);
        }

        private static void Scan()
        {
            foreach (var o in UnityEngine.Object.FindObjectsByType(_smoothType, FindObjectsSortMode.None))
            {
                var b = o as Behaviour;
                if (b == null || !b.enabled || !Mine(b)) continue;
                b.enabled = false;
                Disabled.Add(b);
                _switchedOff++;
            }
            if (Disabled.Count > 4096) Disabled.RemoveAll(x => x == null);
        }

        private static void RestoreAll()
        {
            if (Disabled.Count == 0) return;
            foreach (var b in Disabled) if (b != null && !b.enabled) { b.enabled = true; _restored++; }
            Disabled.Clear();
        }

        private sealed class Runner : MonoBehaviour
        {
            private float _next;

            private void Update()
            {
                try
                {
                    if (!FixManager.IsActive(_self) || !_offline()) { RestoreAll(); return; }
                    if (Time.unscaledTime < _next) return;
                    _next = Time.unscaledTime + 2f;
                    Scan();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("OfflineNetSync error, restoring and pausing: " + e);
                    try { RestoreAll(); } catch { }
                    enabled = false;
                }
            }
        }
    }
}
