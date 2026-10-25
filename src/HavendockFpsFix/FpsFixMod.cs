using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Havendock.Modding;
using UnityEngine;

namespace HavendockFpsFix
{
    public sealed class FpsFixMod : IHavendockMod
    {
        private IModContext _ctx;
        private IDisposable _toggle, _fps, _overlay;
        private const string ActiveKey = "HavendockFpsFix.ActiveCopy";
        private bool _started, _resolverAdded, _claimed, _tickHooked;

        public void OnLoad(IModContext context)
        {
            _ctx = context;
            Plugin.Context = context;
            Plugin.Log = new ModLog(context.Log);
            try { Plugin.ModId = context.Info != null && context.Info.Id != null ? context.Info.Id : ""; } catch { Plugin.ModId = ""; }
            Plugin.Folder = Plugin.FindFolder(context);
            if (!_resolverAdded) { AppDomain.CurrentDomain.AssemblyResolve += ResolveHarmony; _resolverAdded = true; }

            string old = Plugin.FindOldCopy();
            if (old != null)
            {
                Plugin.Log.LogWarning("The BepInEx version of this mod (" + old + ") is also installed, so this copy stays off. Remove the BepInEx version to use this one.");
                try { context.Ui.Toast(Plugin.Name + " is off: please remove the older BepInEx version first (run its Uninstall.cmd)."); } catch { }
                return;
            }

            string running = AppDomain.CurrentDomain.GetData(ActiveKey) as string;
            if (!string.IsNullOrEmpty(running) && running != Plugin.ModId)
            {
                Plugin.Log.LogWarning("Another copy of this mod (" + running + ") is already running, so this copy stays off. Keep only one in the Mods folder.");
                try { context.Ui.Toast(Plugin.Name + ": two copies are installed (" + running + " and " + Plugin.ModId + "). Remove one."); } catch { }
                return;
            }
            AppDomain.CurrentDomain.SetData(ActiveKey, Plugin.ModId);
            _claimed = true;

            try { Start(); _started = true; }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not start, the game runs without the mod: " + e);
                try { Stop(); } catch { }
            }
        }

        public void OnUnload()
        {
            if (_toggle != null) { _toggle.Dispose(); _toggle = null; }
            if (_fps != null) { _fps.Dispose(); _fps = null; }
            if (_overlay != null) { _overlay.Dispose(); _overlay = null; }
            if (_tickHooked) { try { _ctx.Events.Tick -= KeyFallback.OnLoaderTick; } catch { } _tickHooked = false; }
            if (_started)
            {
                try { Stop(); } catch (Exception e) { Plugin.Log.LogWarning("Unload: " + e.Message); }
                _started = false;
            }
            if (_claimed) { AppDomain.CurrentDomain.SetData(ActiveKey, null); _claimed = false; }
            if (_resolverAdded) { AppDomain.CurrentDomain.AssemblyResolve -= ResolveHarmony; _resolverAdded = false; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Start()
        {
            var cfg = new ConfigFile(_ctx.Config);
            Plugin.ShowFpsCounter = cfg.Bind("Display", "ShowFpsCounter", false,
                "Show the FPS counter as soon as you are in a world.");
            FixManager.Init(cfg);

            string legacy = Path.Combine(Plugin.Folder, "legacy-settings.cfg");
            if (File.Exists(legacy))
            {
                try
                {
                    int n = cfg.ImportLegacy(legacy);
                    File.Delete(legacy);
                    Plugin.Log.LogInfo("Imported " + n + " settings from version 1.0.0.");
                }
                catch (Exception e) { Plugin.Log.LogWarning("Could not import the 1.0.0 settings: " + e.Message); }
            }
            cfg.Publish();
            Plugin.Settings = cfg;

            FixManager.ApplyAll();
            FixManager.AddRunner<Overlay>("HavendockFpsFix.Overlay");
            KeyFallback.Toggle = ToggleFixes;
            KeyFallback.FpsCounter = () => Overlay.Show = !Overlay.Show;
            KeyFallback.LastLoaderTick = -1;
            _ctx.Events.Tick += KeyFallback.OnLoaderTick;
            _tickHooked = true;
            FixManager.AddRunner<KeyFallback>("HavendockFpsFix.Keys");

            _toggle = _ctx.Input.RegisterKeybind("toggleFixes", KeyCode.M, ToggleFixes);
            _fps = _ctx.Input.RegisterKeybind("fpsCounter", KeyCode.None, KeyFallback.FpsCounter);
            _overlay = _ctx.Ui.RegisterOverlay(Overlay.Draw);
            Plugin.Log.LogInfo(Plugin.Name + " " + Plugin.Version + " loaded.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Stop()
        {
            Plugin.Settings = null;
            FixManager.Shutdown();
        }

        private void ToggleFixes()
        {
            FixManager.MasterOn = !FixManager.MasterOn;
            string msg = Plugin.Name + ": " + (FixManager.MasterOn ? "ON" : "OFF");
            try { _ctx.Ui.Toast(msg); } catch { }
            Plugin.Log.LogInfo(msg);
        }

        private static Assembly ResolveHarmony(object sender, ResolveEventArgs args)
        {
            string name;
            try { name = new AssemblyName(args.Name).Name; } catch { return null; }
            if (name != "0Harmony") return null;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { if (a.GetName().Name == name) return a; } catch { }
            }
            string path = Path.Combine(Plugin.Folder ?? "", "0Harmony.dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }
    }
}
