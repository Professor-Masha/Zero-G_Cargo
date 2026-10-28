# Validation record

## Verified in the cloud

The .NET 8.0.419 console suite compiles the same engine-independent C# source used by Unity. Current run: **36 executed, 36 passed, 0 failed**.

Coverage includes route clipping/spacing/capacity/projection, manual acceleration/drag/speed cap, drift and wide swings, fixed-step results at 30/60/120 render FPS, swept collision, safe docking and wrong-colour rejection, save clamping/best stars/unlocks, atomic replacement, missing/corrupt/future save recovery, all three collision-free tutorial solution routes and C# 9 syntax parsing for every Unity script.

These are core and source-syntax checks, not a Unity compilation. The three route tests use `tutorials.json`; the committed LevelData assets were generated from the same layouts. Changing a layout requires updating both the asset and its route fixture.

## Required laptop smoke checks (not yet run)

1. Open in Unity 6.6 (6000.6.7f1) and run **Zero-G Cargo → Prepare prototype** twice. Confirm the Console has no compiler/import errors, the second run preserves existing levels/configuration, the 2D URP asset is assigned and TMP default font assets are present. Confirm URP 17.5.0 and uGUI 2.6.0 resolve with no standalone TMP dependency. If setup changes Active Input Handling, restart the editor and confirm mouse/touch and UI work.
2. Open Boot, use portrait 9:16 Game view and press Play. Verify title, text, buttons, coloured triangle, hexagonal bay and grey meteorite render correctly. The scene should advance to MainMenu.
3. Draw away from the shuttle: no route. Tap the shuttle without dragging: no launch. Draw farther than six world units: the route stops extending. Release: one flight starts. A second finger must not replace the active touch on device.
4. Complete all three levels; the third route passes below/right of the central meteorite. Confirm momentum and wide turns are visible. Check that a stopped short route can continue from its current position. At a matching bay, slowing yields one dock and one completion.
5. Hit a meteorite twice. Each collision should spin/return once; the second reveals the three-second guide. Confirm the shuttle is usable after recovery and no route is launched by a canceled touch.
6. Pause during movement, wait, resume: no simulation advance while paused. Open settings/privacy from pause and return. Restart, level select, win→next, final win→level select, and Escape/back should remain navigable.
7. Toggle sound, complete a level, exit Play mode and re-enter. Confirm progress, best stars, unlocks and sound preference persist. To retest first-run behavior, back up then remove the local save files from the platform persistent-data directory. Do not delete user saves blindly.
8. Switch to Android, build the development APK, install it and repeat touch/lifecycle checks. Background/focus loss must save and show pause on return. Test narrow portrait devices and screen cutouts.
9. Use Unity Profiler on device before asserting any performance targets. Android APK/AAB generation, 60 FPS, startup time, memory, app size and three-device-tier testing are currently unverified.

No AdMob/Firebase SDK is installed in this slice. Ad, consent, privacy-site hosting and analytics test matrices remain unrun and cannot be inferred from passing core checks.
