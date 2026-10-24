using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace HavendockFpsFix
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("Havendock.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "havendock.fpsfix";
        public const string Name = "Havendock FPS Fix";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<KeyboardShortcut> KeyToggleFixes;
        internal static ConfigEntry<KeyboardShortcut> KeyFpsCounter;
        internal static ConfigEntry<bool> ShowFpsCounter;

        private void Awake()
        {
            Log = Logger;
            KeyToggleFixes = Config.Bind("Keys", "ToggleFixes", new KeyboardShortcut(KeyCode.F8),
                "Turns the whole mod off or on while you play, so you can compare. Lasts until you quit the game.");
            KeyFpsCounter = Config.Bind("Keys", "ToggleFpsCounter", new KeyboardShortcut(KeyCode.F7),
                "Shows or hides the FPS counter.");
            ShowFpsCounter = Config.Bind("Display", "ShowFpsCounter", false,
                "Show the FPS counter as soon as the game starts.");

            FixManager.Init(Config);
            FixManager.ApplyAll();

            var host = new GameObject("HavendockFpsFix") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(host);
            host.AddComponent<Overlay>();
            Log.LogInfo($"{Name} {Version} loaded.");
        }
    }
}
