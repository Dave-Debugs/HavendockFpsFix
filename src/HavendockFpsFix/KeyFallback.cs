using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class KeyFallback : MonoBehaviour
    {
        internal static int LastLoaderTick = -1;
        internal static Action Toggle;
        internal static Action FpsCounter;

        private float _inWorldSince = -1f, _nextRead;
        private bool _announced;
        private KeyCode _toggleKey = KeyCode.M, _fpsKey = KeyCode.None;
        private static FieldInfo _toolbox, _inputFields, _typing;
        private static bool _typingLookedUp;

        internal static void OnLoaderTick() { LastLoaderTick = Time.frameCount; }

        private void Update()
        {
            try { Tick(); }
            catch (Exception e)
            {
                Plugin.Log.LogError("Key handling error, keys now only work from the mod menu: " + e.Message);
                enabled = false;
            }
        }

        private void Tick()
        {
            var ctx = Plugin.Context;
            bool inWorld = ctx != null && ctx.World != null && ctx.World.InWorld;
            if (!inWorld) { _inWorldSince = -1f; return; }
            if (_inWorldSince < 0f) _inWorldSince = Time.unscaledTime;
            if (Time.frameCount - LastLoaderTick <= 5 || Time.unscaledTime - _inWorldSince < 3f) return;

            if (!_announced)
            {
                _announced = true;
                Plugin.Log.LogWarning("The mod loader is not running mod keys on this game build, so " + Plugin.Name + " reads its own keys.");
            }
            if (Time.unscaledTime >= _nextRead)
            {
                _nextRead = Time.unscaledTime + 1f;
                ReadKeys();
            }
            if (Typing()) return;
            if (_toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey) && Toggle != null) Toggle();
            if (_fpsKey != KeyCode.None && Input.GetKeyDown(_fpsKey) && FpsCounter != null) FpsCounter();
        }

        private void ReadKeys()
        {
            string path = Path.Combine(Plugin.Folder ?? "", "config.json");
            if (!File.Exists(path)) return;
            string text;
            try { text = File.ReadAllText(path); } catch { return; }
            _toggleKey = KeyFor(text, "toggleFixes", KeyCode.M);
            _fpsKey = KeyFor(text, "fpsCounter", KeyCode.None);
        }

        private static KeyCode KeyFor(string json, string name, KeyCode fallback)
        {
            var m = Regex.Match(json, "\"name\"\\s*:\\s*\"" + Regex.Escape(name) + "\"\\s*,\\s*\"key\"\\s*:\\s*\"([^\"]*)\"");
            if (!m.Success) return fallback;
            string value = m.Groups[1].Value;
            if (value.Length == 0) return KeyCode.None;
            try { return (KeyCode)Enum.Parse(typeof(KeyCode), value, true); }
            catch { return fallback; }
        }

        private static bool Typing()
        {
            if (!_typingLookedUp)
            {
                _typingLookedUp = true;
                var t = AccessTools.TypeByName("Toolbox");
                _toolbox = t != null ? AccessTools.Field(t, "instance") : null;
                _inputFields = t != null ? AccessTools.Field(t, "inputFieldManager") : null;
                var it = AccessTools.TypeByName("InputFieldManager");
                _typing = it != null ? AccessTools.Field(it, "isTypingInInputField") : null;
            }
            if (_toolbox == null || _inputFields == null || _typing == null) return false;
            var tb = _toolbox.GetValue(null) as UnityEngine.Object;
            if (tb == null) return false;
            var ifm = _inputFields.GetValue(tb) as UnityEngine.Object;
            if (ifm == null) return false;
            return (bool)_typing.GetValue(ifm);
        }
    }
}
