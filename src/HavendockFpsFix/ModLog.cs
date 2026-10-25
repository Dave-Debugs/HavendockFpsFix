using System;
using Havendock.Modding;
using UnityEngine;

namespace HavendockFpsFix
{
    internal sealed class ModLog
    {
        private readonly IModLog _log;

        internal ModLog(IModLog log)
        {
            _log = log;
        }

        public void LogInfo(string s)
        {
            try { if (_log != null) { _log.Info(s); return; } } catch { }
            Fallback(s);
        }

        public void LogWarning(string s)
        {
            try { if (_log != null) { _log.Warn(s); return; } } catch { }
            Fallback(s);
        }

        public void LogError(string s)
        {
            try { if (_log != null) { _log.Error(s); return; } } catch { }
            Fallback(s);
        }

        private static void Fallback(string s)
        {
            try { Debug.Log("[" + Plugin.Name + "] " + s); } catch { }
        }
    }
}
