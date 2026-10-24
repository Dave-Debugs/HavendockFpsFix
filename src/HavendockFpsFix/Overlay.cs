using System;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class Overlay : MonoBehaviour
    {
        private readonly float[] _frames = new float[600];
        private readonly float[] _sorted = new float[600];
        private int _count, _index, _accFrames;
        private float _accTime;
        private bool _show;
        private string _text = "";
        private string _toast = "";
        private float _toastUntil;
        private GUIStyle _style;

        private void Awake()
        {
            _show = Plugin.ShowFpsCounter.Value;
        }

        private void Update()
        {
            if (Plugin.KeyToggleFixes.Value.IsDown())
            {
                var cfg = FixManager.MasterEnabled.ConfigFile;
                bool save = cfg.SaveOnConfigSet;
                cfg.SaveOnConfigSet = false;
                FixManager.MasterEnabled.Value = !FixManager.MasterEnabled.Value;
                cfg.SaveOnConfigSet = save;
                _toast = "Havendock FPS Fix: " + (FixManager.MasterEnabled.Value ? "ON" : "OFF");
                _toastUntil = Time.unscaledTime + 2.5f;
                Plugin.Log.LogInfo(_toast);
            }
            if (Plugin.KeyFpsCounter.Value.IsDown()) _show = !_show;
            if (!_show) return;

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
            _text = string.Format("{0:F0} FPS   1% low {1:F0}   mod {2}", avg, low, FixManager.MasterEnabled.Value ? "ON" : "OFF");
            _accTime = 0f;
            _accFrames = 0;
        }

        private void OnGUI()
        {
            bool toast = Time.unscaledTime < _toastUntil;
            if (!_show && !toast) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
                _style.normal.textColor = Color.white;
            }
            if (_show && _text.Length > 0) Box(new Rect(Screen.width - 300, 8, 292, 28), _text);
            if (toast) Box(new Rect(Screen.width / 2f - 150, 8, 300, 28), _toast);
        }

        private void Box(Rect r, string text)
        {
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(r, text, _style);
        }
    }
}
