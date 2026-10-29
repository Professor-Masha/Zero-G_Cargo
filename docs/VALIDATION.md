# Validation status — 50-level 3D build candidate

## Executed in the cloud

- .NET 8.0.419 compiles the engine-independent C# used by Unity: **96 executed, 96 passed, 0 failed** on the latest run.
- All **50** authored campaign guides complete without collision or timeout, including every cargo and chained bay dependency. This is a functional simulation test, not a zero-test or data-only run.
- Existing three prototype guide regressions, acceleration/drag/speed, drift, turns, swept circle/box collisions, fixed-step rendering cadence, safe/incorrect docking, timer failure, one-shot events, force directions, lock dependencies, malformed content, save validation/backup/oversize handling and all Unity-source C# syntax parsing execute.
- Blender 4.3.2 exported nine FBX model assets and rendered the model preview from the editable source. No Unity rendering is implied by that preview.

## Required before calling the game finished

1. Open the updated project in Unity 6.6 (6000.6.7f1), restore actual LFS assets and run **Prepare game** twice. Verify the 3D pipeline and lit/unlit shader templates, TMP essentials and both generated font assets. This source migration has not been compiled against Unity assemblies here.
2. Open Boot and play at 9:16, 16:9 and narrow portrait. Verify the FBX axis/scale imports, deck, lights, materials, fonts, menu texture, header/instructions, scrollable fifty-level map and timer arc. No missing or pink materials, clipped buttons or obscured input are acceptable.
3. Complete levels from each tier by touch. Check single-pointer ownership, pointer cancellation, simultaneous cargo flights, route budgets, continuing stopped paths, meteorite/ship/barrier collisions, gravity, docking feedback, locked bays and pauses. The sample guides establish solvability, not ergonomic input or difficulty.
4. Verify pause/settings/privacy/reset/win/fail/back navigation, persistent stars/unlocks/preferences and lifecycle background/focus loss. Fail offers exactly Try again and Level select, with no ads. Recheck resetting all preferences and audio.
5. Install Android modules and build using **Build phone test APK** or `scripts/build-phone-apk.ps1`. Verify an actual signed APK exists and inspect its merged manifest: no unexpected network/storage permissions, no cleartext, no backup, no debuggable player. Check signing and SHA-256.
6. Install on ARM64 Android 8.0+ devices; complete all fifty levels at least once on the reference device, then human-playtest adjacent-level difficulty and engagement. Test low/mid/high memory tiers and phone interruptions.
7. Profile frame rate, allocation trends, memory, startup time and file size on device. The original performance targets have not been established. Review Unity/package vulnerabilities before distribution.

## Current external blocker

The cloud has no Unity Editor, Android Unity build modules or Unity licence; the user's laptop does. **No APK has been produced or tested.** A licensed Unity build and its logs are required to resolve import/compiler/device failures and deliver a verified phone binary. Source and simulation success are not equivalent to an APK or complete release.

AdMob/Firebase, store signing, Google Play release, hosted privacy policy and their integration/policy matrices are not included in the offline phone-test candidate and remain unverified work from the original release specification.
