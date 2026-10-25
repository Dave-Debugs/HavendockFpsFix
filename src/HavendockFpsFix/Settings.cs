using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Havendock.Modding;

namespace HavendockFpsFix
{
    internal sealed class ConfigFile
    {
        internal interface IEntry
        {
            string Section { get; }
            string OldKey { get; }
            string Key { get; }
            bool Visible { get; }
            void Reload();
            void Publish();
            bool Import(string raw);
        }

        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "Fixes.NearestSpotIndex", "fastNearestSearch" },
            { "Fixes.WorldCanvasCulling", "hideOffscreenIcons" },
            { "Fixes.EmptyCanvasDormant", "sleepEmptyPanels" },
            { "Fixes.HudNewTagToggle", "quietNewTags" },
            { "Fixes.QualityTweaks", "shadowTweaks" },
            { "Fixes.AnimatorCulling", "pauseOffscreenAnims" },
            { "QualityTweaks.ShadowCascades", "shadowCascades" },
            { "AnimatorCulling.IncludeAgents", "includeSettlers" },
            { "Fixes.AnimatorEvents", "skipUnheardAnimEvents" },
            { "Fixes.OfflineNetSync", "pauseOfflineNetSync" },
            { "Fixes.LeanHoverCheck", "leanHoverCheck" },
            { "PostEffects.Off", "postEffectsOff" },
            { "Fixes.WaterReflectionShadows", "noReflectionShadows" },
            { "Display.ShowFpsCounter", "showFpsCounter" },
        };

        private readonly IModConfig _store;
        private readonly List<IEntry> _entries = new List<IEntry>();

        internal ConfigFile(IModConfig store)
        {
            _store = store;
        }

        internal IModConfig Store { get { return _store; } }

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            string name;
            bool visible = Names.TryGetValue(section + "." + key, out name);
            if (!visible) name = Lower(section) + "." + Lower(key);
            var e = new ConfigEntry<T>(this, section, key, name, defaultValue, visible);
            e.Reload();
            _entries.Add(e);
            return e;
        }

        internal void Publish()
        {
            foreach (var e in _entries) if (e.Visible) e.Publish();
        }

        internal void Reload()
        {
            foreach (var e in _entries) e.Reload();
        }

        internal int ImportLegacy(string path)
        {
            var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string section = "";
            foreach (string line in File.ReadAllLines(path))
            {
                string t = line.Trim();
                if (t.Length == 0 || t[0] == '#' || t[0] == ';') continue;
                if (t[0] == '[' && t[t.Length - 1] == ']') { section = t.Substring(1, t.Length - 2).Trim(); continue; }
                int eq = t.IndexOf('=');
                if (eq <= 0) continue;
                raw[section + "." + t.Substring(0, eq).Trim()] = t.Substring(eq + 1).Trim();
            }
            int n = 0;
            foreach (var e in _entries)
            {
                string v;
                if (raw.TryGetValue(e.Section + "." + e.OldKey, out v) && e.Import(v)) n++;
            }
            return n;
        }

        private static string Lower(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }

    internal sealed class ConfigEntry<T> : ConfigFile.IEntry
    {
        private readonly ConfigFile _file;

        public string Section { get; private set; }
        public string OldKey { get; private set; }
        public string Key { get; private set; }
        public bool Visible { get; private set; }
        public T DefaultValue { get; private set; }
        public T Value { get; set; }

        internal ConfigEntry(ConfigFile file, string section, string oldKey, string key, T defaultValue, bool visible)
        {
            _file = file;
            Section = section;
            OldKey = oldKey;
            Key = key;
            DefaultValue = defaultValue;
            Value = defaultValue;
            Visible = visible;
        }

        public void Reload()
        {
            try { Value = Read(); }
            catch { Value = DefaultValue; }
        }

        public void Publish()
        {
            try { Write(Value); } catch { }
        }

        public bool Import(string raw)
        {
            try
            {
                var v = (T)Convert.ChangeType(raw.Trim(), typeof(T), CultureInfo.InvariantCulture);
                Write(DefaultValue);
                Write(v);
                Value = v;
                return true;
            }
            catch { return false; }
        }

        private T Read()
        {
            var c = _file.Store;
            object d = DefaultValue;
            if (typeof(T) == typeof(bool)) return (T)(object)c.Get(Key, (bool)d);
            if (typeof(T) == typeof(int)) return (T)(object)c.Get(Key, (int)d);
            if (typeof(T) == typeof(float)) return (T)(object)c.Get(Key, (float)d);
            return (T)(object)c.Get(Key, Convert.ToString(d, CultureInfo.InvariantCulture));
        }

        private void Write(T v)
        {
            var c = _file.Store;
            object o = v;
            if (typeof(T) == typeof(bool)) c.Set(Key, (bool)o);
            else if (typeof(T) == typeof(int)) c.Set(Key, (int)o);
            else if (typeof(T) == typeof(float)) c.Set(Key, (float)o);
            else c.Set(Key, Convert.ToString(o, CultureInfo.InvariantCulture));
        }
    }
}
