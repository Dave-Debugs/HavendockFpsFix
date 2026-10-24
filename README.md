# Havendock FPS Fix

**More FPS for big late-game colonies.**

Havendock slows down a lot once your colony gets big. This mod removes a lot of work the game repeats every frame for no visible reason, so large colonies run noticeably smoother.

| Large late-game save | Average FPS | 1% low |
|---|---|---|
| Without the mod | ~52 | ~47 |
| With the mod | ~68 | ~58 |

Your result depends on your PC and the size of your colony.

- Your saves are never touched
- No game files are changed
- Removes completely with one click

## Download

Get **HavendockFpsFix-1.0.0.zip** from the [Releases page](https://github.com/Dave-Debugs/HavendockFpsFix/releases/latest). Don't use the green "Code" button, that's the source code.

## Install

1. Close Havendock.
2. Unzip the download anywhere (your Desktop is fine).
3. Double-click **Install.cmd**.
4. Start Havendock from Steam like you normally do.

The installer finds the game through Steam on its own. If Windows shows a blue "Windows protected your PC" box, click **More info** and then **Run anyway**.

## In game

| Key | What it does |
|---|---|
| **F8** | Turn the mod off / on to see the difference |
| **F7** | Show / hide an FPS counter |

## Remove

Close Havendock and double-click **Uninstall.cmd**. Everything the installer added is removed again.

## What it changes

- Floating icons and panels above buildings are switched off while they are off screen or empty. They come back the moment they are needed.
- Settlers find the nearest bed, food, drink and so on much faster. They still pick the same places.
- The toolbar's NEW tags no longer rebuild the whole HUD every frame.
- Animations you can't see (off screen) are paused until they come back into view.
- Sun shadows use 2 detail levels instead of 4.

Things you might notice: shadows far from the camera are a little softer, and the small wiggle on the toolbar's NEW tags now plays.

Want even more FPS? Turn off water reflections in the game's own settings menu. The mod leaves that choice to you.

## Good to know

- Made for the Steam version of Havendock (October 2026). If a game update changes something the mod relies on, that part switches itself off and the game runs normally.
- Tested in single player. Co-op has not been tested.
- Works alongside other BepInEx mods. If you already have BepInEx, the installer leaves it alone.
- Every fix can be switched off on its own in `Havendock\BepInEx\config\havendock.fpsfix.cfg` (created after the first start).
- Something looks wrong? Press F8. If the problem goes away, it's the mod. Please [open an issue](https://github.com/Dave-Debugs/HavendockFpsFix/issues) and describe what you saw.

## Building from source

You need Windows, the [.NET SDK](https://dotnet.microsoft.com/download) (8 or newer) and Havendock with BepInEx installed (running Install.cmd once is enough).

```powershell
.\build.ps1
```

The script finds the game through Steam, builds the mod and puts the release zip in `dist\`. Use `.\build.ps1 -GameDir "D:\Games\Havendock"` if the game is somewhere else.

## Licence

The mod's code is released under the [MIT licence](LICENSE). BepInEx and its libraries keep their own licences (see `installer/files/THIRD-PARTY-LICENSES.txt`).

## Credits

Uses [BepInEx](https://github.com/BepInEx/BepInEx) 5.4.23.5 as the mod loader. Havendock is made by YYZ Productions. This mod is unofficial and contains no game code or assets.
