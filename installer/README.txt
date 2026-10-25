==============================================================
  HAVENDOCK FPS FIX  2.0.0
  More FPS for big late-game colonies
==============================================================

When a colony gets big, Havendock starts to stutter. A lot of
that comes from work the game repeats every frame even though
nothing on screen changes. This mod skips that work. Your
colony, your saves and how the game plays stay the same.

Tested on a large late-game colony (2560x1440, Ultra, water
reflections on):

    without the mod   about 52 FPS   (1% low about 47)
    with the mod      about 91 FPS   (1% low about 79)

Your result depends on your PC and the size of your colony.


--------------------------------------------------------------
  HOW TO INSTALL
--------------------------------------------------------------

  1. Close Havendock.
  2. Unzip this download (right-click it > Extract All).
  3. Open the unzipped folder and double-click  Install.cmd
  4. Start Havendock and load your world.
  5. Wait a few seconds, then press M. A message says
     "Havendock FPS Fix: OFF". Press M again to turn it on.

  If Windows shows a blue "Windows protected your PC" box,
  click "More info" and then "Run anyway".

  The installer finds Havendock through Steam by itself. If it
  can't, it asks for the game folder. In Steam: right-click
  Havendock > Manage > Browse local files, copy the address.

  Havendock mods need Havendock's mod loader. If it is missing,
  the installer downloads the official one for you. If it says
  it can't, install the free Havendock Mod Manager from
  tools.havendock.online once, let it set up the mod loader,
  then run Install.cmd again.

  Had version 1.0.0? Just run Install.cmd. It replaces the old
  version and keeps your settings.

  By hand: copy the folder  files\{MODID}  into
  Havendock\Mods\  (the mod loader is still needed).


--------------------------------------------------------------
  IN GAME
--------------------------------------------------------------

  M             Turn the mod off / on to see the difference
  Pause/Break   Open the mod menu

  In the mod menu, under Havendock FPS Fix, you can turn on an
  FPS counter, change the M key, and switch each fix off or on
  by itself under Settings. Changes work within a second.


--------------------------------------------------------------
  WHAT IT CHANGES
--------------------------------------------------------------

  - Floating icons and panels above buildings switch off while
    they are off screen or empty, and come back the moment
    they are needed.
  - Settlers find the nearest bed, food, drink and so on much
    faster. They still pick the same places.
  - The toolbar's NEW tags stop redrawing the toolbar every
    frame.
  - Animations you can't see pause until they are back in view.
  - Sun shadows use 2 detail levels instead of 4.
  - Animations stop sending messages nothing listens to,
    network smoothing pauses in single player (it only matters
    in co-op), and the mouse-over check stops making garbage
    every frame.
  - The water's reflection is drawn without shadows.
  - Optional, off unless you fill it in: "Post effects off"
    switches off screen effects you name, e.g. Bloom.

  Things you might notice:
  - Shadows far from the camera are a little softer.
  - Things reflected in the water have no shadows.
  - The small wiggle on the toolbar's NEW tags now plays.

  Want a little more? In the game's own settings, Depth of
  Field off and water reflections off both help a bit more.
  They change the look, so the mod leaves them to you.


--------------------------------------------------------------
  IF SOMETHING IS WRONG
--------------------------------------------------------------

  - M does nothing: wait a few seconds after the world has
    loaded and try again. Press Pause/Break. If Havendock FPS
    Fix is not in the mod menu, run Install.cmd again and read
    what it says.
  - Something looks odd: press M. If it goes away, it's the
    mod. Switch off the matching setting in the mod menu and
    please report it at {REPORT}
  - After a game update: if a fix no longer fits the game, it
    switches itself off and the game runs normally.


--------------------------------------------------------------
  HOW TO REMOVE
--------------------------------------------------------------

  Close Havendock and double-click  Uninstall.cmd
  Or delete the folder  Havendock\Mods\{MODID}

  The mod loader stays, because other Havendock mods use it.
  To remove it too, delete  Havendock\BepInEx\plugins\HavendockLoader


--------------------------------------------------------------
  CREDITS
--------------------------------------------------------------

  Uses Harmony 2.4.3 (unchanged official release, MIT licence).
  Licences are in files\THIRD-PARTY-LICENSES.txt.
  Havendock is made by YYZ Productions. This mod is unofficial
  and contains no game code or assets.
