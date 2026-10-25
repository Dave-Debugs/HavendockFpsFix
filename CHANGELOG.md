# Changelog

## 2.0.0

Now runs on Havendock's own mod loader (modding API 8 or newer) instead of being a BepInEx plugin.

- Installs into `Havendock\Mods\dave-debugs.fpsfix`; works on the co-op build and on any copy with the Havendock Mod Loader
- Install.cmd sets up the official Havendock mod loader if it is missing (when BepInEx 5 is in the game folder)
- Install.cmd replaces version 1.0.0 and keeps your settings
- F7 and F8 are gone (the game uses every F-key): **M** now switches the fixes off and on, and the FPS counter is in the mod menu (Pause/Break); both keys can be changed there
- M also works on game copies where the mod loader's own key handling does not run
- Measured on a large late-game colony: about 52 to 91 FPS (1% low about 47 to 79)
- Every fix has its own switch under Settings in the mod menu; changes take effect within a second
- The FPS counter can be moved with Arrange HUD
- New: animations stop sending events that no script listens for
- New: network smoothing pauses in single player (it does nothing without other players) and comes back as soon as the game goes online
- New: the every-frame mouse-over-UI check no longer makes garbage, so there are fewer small stutters
- New: the water's reflection is drawn without shadows (the game has no setting for this; the water keeps its reflections)
- New, optional: "Post effects off" switches off chosen post-processing effects (empty by default because it changes the look)
- The 1.0.0 fixes are unchanged

## 1.0.0

First release.

- Floating UI above buildings sleeps while it is off screen or empty
- Faster nearest-building search for settlers (same results)
- Toolbar NEW tags no longer rebuild the HUD every frame
- Off-screen decorative animations pause until they are visible again
- Sun shadows use 2 cascades instead of 4
- F8 turns the mod off/on in game, F7 shows an FPS counter
- One-click install and uninstall
