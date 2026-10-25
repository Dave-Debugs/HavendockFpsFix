using System;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class Overlay : MonoBehaviour
    {
        internal static bool Show;
        private static string _text = "";

        private readonly float[] _frames = new float[600];
        private readonly float[] _sorted = new float[600];
        private int _count, _index, _accFrames;
        private float _accTime, _nextReload;
        private bool _lastSetting;

        private void Awake()
        {
            _lastSetting = Plugin.ShowFpsCounter != null && Plugin.ShowFpsCounter.Value;
            Show = _lastSetting;
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextReload)
            {
                _nextReload = Time.unscaledTime + 1f;
                try
                {
                    var s = Plugin.Settings;
                    if (s != null) s.Reload();
                    bool want = Plugin.ShowFpsCounter != null && Plugin.ShowFpsCounter.Value;
                    if (want != _lastSetting) { _lastSetting = want; Show = want; }
                }
                catch { }
            }

            if (!Show) { _count = 0; _accTime = 0f; _accFrames = 0; return; }

            float dt = Time.unscaledDeltaTime;
            _frames[_index] = dt;
            _index = (_index + 1) % _frames.Length;
            if (_count < _frames.Length) _count++;
            _accTime += dt;
            _accFrames++;
            if (_accTime < 0.5f) return;

            float avg = _accFrames / _accTime;
            Array.Copy(_frames, _sorted, _count);
            Array.Sort(_sorted, 0, _count);
            int n = Math.Max(1, _count / 100);
            float slow = 0f;
            for (int i = _count - n; i < _count; i++) slow += _sorted[i];
            float low = n / slow;
            _text = string.Format("{0:F0} FPS   1% low {1:F0}   mod {2}", avg, low, FixManager.MasterOn ? "ON" : "OFF");
            _accTime = 0f;
            _accFrames = 0;
        }

        internal static void Draw()
        {
            var ctx = Plugin.Context;
            if (ctx == null) return;
            var ui = ctx.Ui;
            bool editing = ui.HudEditing;
            if (!editing && (!Show || _text.Length == 0)) return;
            var st = ui.Styles;
            float s = st.Scale;
            var r = ui.Hud("fps", new Rect(Screen.width - 330f * s, 8f * s, 320f * s, 30f * s), new Vector2(200f * s, 24f * s));
            GUI.Box(r, GUIContent.none, st.Box);
            string text = Show && _text.Length > 0 ? _text : "FPS counter";
            GUI.Label(new Rect(r.x + 10f * s, r.y, r.width - 20f * s, r.height), text, st.Label);
        }
    }
}
