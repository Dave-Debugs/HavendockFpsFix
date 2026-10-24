==============================================================
  HAVENDOCK FPS FIX  1.0.0
  More FPS for big late-game colonies
==============================================================

Havendock slows down a lot once your colony gets big. This mod
removes a lot of work the game repeats every frame for no
visible reason, so big colonies run noticeably smoother.

Tested on a large late-game save: about 52 FPS -> about 68 FPS
(roughly +30%), with smoother 1% lows. Your result depends on
your PC and the size of your colony.

Your saves are never touched, no game files are changed, and
it can be removed completely at any time.


--------------------------------------------------------------
  HOW TO INSTALL
--------------------------------------------------------------

  1. Close Havendock.
  2. Unzip this folder anywhere (your Desktop is fine).
  3. Double-click  Install.cmd
  4. Start Havendock from Steam like you normally do.

  The installer finds Havendock through Steam on its own. If it
  can't, it asks you to paste the game folder.

  If Windows shows a blue "Windows protected your PC" box,
  click "More info" and then "Run anyway".


--------------------------------------------------------------
  IN GAME
--------------------------------------------------------------

  F8   Turn the mod off / on (to see the difference yourself)
  F7   Show / hide an FPS counter


--------------------------------------------------------------
  HOW TO REMOVE
--------------------------------------------------------------

  Close Havendock and double-click  Uninstall.cmd
  Everything the installer added is removed again.


--------------------------------------------------------------
  WHAT IT CHANGES
--------------------------------------------------------------

  - Floating icons and panels above buildings are switched off
    while they are off screen or empty. They come back the
    moment they are needed.
  - Settlers find the nearest bed, food, drink and so on much
    faster. They still pick the same places.
  - The toolbar's NEW tags no longer rebuild the whole HUD
    every frame.
  - Animations you can't see (off screen) are paused until
    they come back into view.
  - Sun shadows use 2 detail levels instead of 4.

  Things you might notice:
  - Shadows far from the camera are a little softer.
  - The small wiggle on the toolbar's NEW tags now plays.

  Want even more FPS? Turn off water reflections in the game's
  own settings menu. That one is up to you, so the mod leaves
  it alone.


--------------------------------------------------------------
  GOOD TO KNOW
--------------------------------------------------------------

  - Made for the Steam version of Havendock (October 2026).
    If a game update changes something the mod relies on, that
    part switches itself off and the game runs normally.
  - Tested in single player. Co-op has not been tested.
  - Already use other BepInEx mods? That's fine. The installer
    leaves your BepInEx alone.
  - Settings file (created after the first start):
      Havendock\BepInEx\config\havendock.fpsfix.cfg
    Every fix can be switched off there on its own.
  - Something looks wrong? Press F8. If the problem goes away,
    it's the mod. Please report it at
    github.com/Dave-Debugs/HavendockFpsFix/issues


--------------------------------------------------------------
  CREDITS
--------------------------------------------------------------

  Uses BepInEx 5.4.23.5 (mod loader, unchanged official
  release). Licences are in files\THIRD-PARTY-LICENSES.txt.
  Havendock is made by YYZ Productions. This mod is unofficial
  and contains no game code or assets.
