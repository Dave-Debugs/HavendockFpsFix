# Havendock FPS Fix

**More FPS for big late-game colonies in Havendock.**

When a colony gets big, Havendock starts to stutter. A lot of that comes from work the game repeats every single frame even though nothing on screen changes. This mod skips that work. Your colony, your saves and how the game plays stay exactly the same.

| Large late-game colony (2560x1440, Ultra, water reflections on) | Average FPS | 1% low |
|---|---|---|
| Without the mod | about 52 | about 47 |
| With the mod | about 91 | about 79 |

These numbers come from one PC. Yours will differ, but the bigger your colony, the more it helps.

- Your saves are never changed
- No game files are changed
- Turn it off at any time with one key
- Remove it completely with one click

---

## What you need

- **Havendock** on Windows (Steam, Epic or GOG).
- **Havendock's mod loader.** You probably don't need to do anything here:
  - If your game is patched for co-op, you already have it.
  - Otherwise the installer below sets it up for you.
  - If the installer says it can't, install the free **Havendock Mod Manager** from [tools.havendock.online](https://tools.havendock.online/) once and let it set up the mod loader. Then run the installer again.

---

## Install

**Option 1: Havendock Mod Manager (easiest).** Once Havendock FPS Fix is listed on [mods.havendock.online](https://mods.havendock.online/), find it in the Mod Manager and click install.

**Option 2: download from here.**

1. Open the [Releases page](https://github.com/Dave-Debugs/HavendockFpsFix/releases/latest).
2. Under **Assets**, click **HavendockFpsFix-2.0.0.zip** to download it. (Don't use the green "Code" button. That's the source code, not the mod.)
3. Right-click the downloaded file and choose **Extract All**.
4. Close Havendock if it's running.
5. Open the extracted folder and double-click **Install.cmd**.
   - If Windows shows a blue "Windows protected your PC" box, click **More info**, then **Run anyway**.
   - The installer finds Havendock through Steam by itself. If it can't, it asks you to paste the game folder. In Steam: right-click Havendock, then **Manage**, then **Browse local files**, and copy the address.
6. Start Havendock and load your world.

To check it worked, wait a few seconds after your world has loaded and press **M**. A message says "Havendock FPS Fix: OFF". Press **M** again to turn it back on.

Had version 1.0.0? Just run the new Install.cmd. It replaces the old version and keeps your settings.

**Option 3: by hand.** Copy the folder `files\dave-debugs.fpsfix` from the download into `Havendock\Mods\`. You need the mod loader for this (see "What you need").

---

## Using it

| Key | What it does |
|---|---|
| **M** | Turn the mod off and on, so you can see the difference |
| **Pause/Break** | Open the mod menu |

In the mod menu, under **Havendock FPS Fix**, you can:

- turn on an **FPS counter** (move it with **Arrange HUD**)
- change the **M** key to another key
- switch each fix off or on by itself, under **Settings**

| Setting | Normally | What it does |
|---|---|---|
| Fast nearest search | on | Settlers find the nearest bed, food, drink and so on much faster. They still pick the same places. |
| Hide offscreen icons | on | The floating icons above buildings are switched off while they are off screen. |
| Sleep empty panels | on | Floating panels that show nothing go to sleep until they have something to show. |
| Quiet new tags | on | The NEW tags on the toolbar stop redrawing the whole toolbar every frame. |
| Shadow tweaks | on | Uses the "Shadow cascades" value below. Switch it off to get the game's own shadows back. |
| Shadow cascades | 2 | How detailed sun shadows are: 1, 2 or 4 (the game uses 4). 0 means "leave the game's setting". |
| Pause offscreen anims | on | Animations you can't see pause until they come back into view. |
| Include settlers | on | Also pauses off-screen settler and animal animations. Their walking is not affected. |
| Skip unheard anim events | on | Animations stop sending messages that nothing listens to. |
| Pause offline net sync | on | In single player, pauses network smoothing that only matters in co-op. It switches back on by itself when you play online. |
| Lean hover check | on | The "is the mouse over a menu?" check stops making garbage every frame, so there are fewer small stutters. |
| No reflection shadows | on | The water's reflection is drawn without shadows. The water and everything else look the same. |
| Post effects off | empty | Optional. Names of screen effects to switch off, separated by commas, for example `AmbientOcclusion, Bloom`. This one changes the look, so it does nothing unless you fill it in. |
| Show fps counter | off | Show the FPS counter as soon as a world loads. |

Changes to settings take effect within a second. They are stored in `Havendock\Mods\dave-debugs.fpsfix\config.json`.

---

## What you might notice

- Shadows far from the camera are a little softer.
- Things reflected in the water have no shadows.
- The small wiggle on the toolbar's NEW tags now plays.

Want a little more FPS? Two of the game's own settings help: **Depth of Field off** was worth about 0.5 ms per frame in testing, and **water reflections off** about 1.5 ms. Both change the look, so the mod leaves them to you.

---

## If something is wrong

- **Pressing M does nothing.** Wait a few seconds after the world has finished loading, then try again. Press **Pause/Break**: if Havendock FPS Fix is not listed in the mod menu, run Install.cmd again and read what it says.
- **Something looks odd.** Press **M**. If the problem goes away, it's the mod. Switch off the setting that matches what you saw, and please [open an issue](https://github.com/Dave-Debugs/HavendockFpsFix/issues) describing it.
- **The installer says the mod loader is missing.** Install the Havendock Mod Manager from [tools.havendock.online](https://tools.havendock.online/), let it set up the mod loader, then run Install.cmd again.
- **After a game update.** If an update changes something a fix relies on, that fix switches itself off and the game simply runs normally.

---

## Remove

Close Havendock and double-click **Uninstall.cmd** from the download. Or delete the folder `Havendock\Mods\dave-debugs.fpsfix`.

The mod loader stays, because other Havendock mods use it too. To remove it as well, delete `Havendock\BepInEx\plugins\HavendockLoader`.

---

## Multiplayer

This is a client mod. It only changes how your own game draws things and looks them up, never the shared game world, so it is safe on any server you join. Tested in single player.

---

## Building from source

You need Windows, the [.NET SDK](https://dotnet.microsoft.com/download) (8 or newer) and a copy of Havendock with the mod loader.

```powershell
.\build.ps1          # build and install into Havendock\Mods\dave-debugs.fpsfix
.\build.ps1 -Zip     # also make the release zip and the mod zip in dist\
.\build.ps1 -NoInstall -Zip
```

The script finds the game through Steam (or use `-HavendockDir "D:\Games\Havendock"`, or set `HAVENDOCK_DIR`). It downloads Harmony 2.4.3 from NuGet once, checks its checksum, then builds against the game's own `HavendockModding.dll`. The project follows the official [Havendock Modding Kit](https://tools.havendock.online/).

The modding API has no hooks for these engine-side costs, so the mod uses [Harmony](https://github.com/pardeike/Harmony) (shipped next to it). Every patch checks the game's code first, and stands down by itself if a game update changed it.

---

## Licence and credits

The mod's code is released under the [MIT licence](LICENSE). Harmony keeps its own MIT licence (included as `Harmony-LICENSE.txt`).

Uses [Harmony](https://github.com/pardeike/Harmony) 2.4.3. Havendock is made by YYZ Productions. This mod is unofficial and contains no game code or assets.
