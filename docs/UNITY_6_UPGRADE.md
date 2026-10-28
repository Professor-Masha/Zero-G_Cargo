# Unity 6.6 migration

The project now targets **Unity Editor 6000.6.7f1 (Unity 6.6)**, the Recommended version shown in the supplied Unity Hub screenshot. Hub itself can remain current; it launches the editor version selected for a project.

## On your Windows laptop

1. Close Unity Editor. Back up your existing project folder if you have local changes. Keep your previous copy until this version runs successfully.
2. Install **Unity 6.6 / 6000.6.7f1** from **Unity Hub → Installs → Install Editor**. Include Android Build Support, Android SDK & NDK Tools and OpenJDK for Android development. Wait until Hub reports installation complete.
3. Get the updated project. For a ZIP download, extract the new ZIP into a **new folder** instead of overwriting the old one. If using Git, commit/stash your local work as appropriate, then pull `master` with `git pull --ff-only`.
4. In **Unity Hub → Projects → Add → Add project from disk**, select the updated folder containing `Assets`, `Packages`, and `ProjectSettings`. If Hub still lists the old copy, check the path carefully. Use the project's editor-version selector to choose **6000.6.7f1**.
5. Open the project, accept the upgrade/API updater prompt if shown, and wait for package resolution/import to finish. Preparation normally runs automatically; also run **Zero-G Cargo → Prepare prototype** from the editor's menu.
6. If preparation logs that Active Input Handling changed, close and reopen the editor. The prototype uses **Edit → Project Settings → Player → Other Settings → Configuration → Active Input Handling → Input Manager (Old)**. Do not select Input System Package (New) only.
7. Open `Assets/_Project/Scenes/Boot/Boot.unity`, choose a portrait 9:16 Game view, and press Play. Complete all three tutorials and follow `VALIDATION.md` before attempting an Android build.

Use **File → Build Profiles** for the Android target. The project's **Zero-G Cargo → Build Android development APK** command remains available after Android modules are installed and Android is the active target.

If upgrading an existing checkout causes stale package errors, close Unity and first preserve local changes. `Library` is a generated cache that Unity can recreate; remove only that folder if needed, then reopen. Do not remove `Assets`, `ProjectSettings`, or local save files. Let Unity regenerate and review `Packages/packages-lock.json` after a successful resolution; no lockfile was fabricated in the cloud.

## Changes

- Editor pin: 2022.3.62f1 → **6000.6.7f1**.
- URP: 14.0.11 → **17.5.0**. Unity's official URP changelog identifies 17.5.0 as compatible with Unity 6000.6; 17.6.0 is associated with Unity 6000.7, so package versions should not be inferred from editor minor numbers.
- uGUI: 1.0.0 → **2.6.0**, as declared on Unity's official `6000.6` uGUI branch. This includes TMP; the standalone `com.unity.textmeshpro` dependency is removed.
- The unused legacy Unity Test Framework dependency is removed. This project currently uses the .NET console test runner and has no Unity test assemblies.
- Font preparation uses TMP's package locator and the current package import API, rather than hard-coding the removed standalone package directory.
- Android identifiers/backend selection use `NamedBuildTarget.Android`. Preparation explicitly retains the legacy Input Manager required by existing input/UI code.
- Gameplay assets, scene GUIDs, simulation formulas and local-save schema are preserved. Unity-generated renderer/font assets from an earlier laptop import should be allowed to upgrade normally; preparation preserves existing assets.

Official source references used for migration:

- [URP changelog](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.universal/CHANGELOG.md)
- [Unity 6.6 uGUI package metadata](https://github.com/Unity-Technologies/uGUI/blob/6000.6/com.unity.ugui/package.json)
- [TMP editor resource importer](https://github.com/Unity-Technologies/uGUI/blob/6000.6/com.unity.ugui/Editor/TMP/TMP_PackageUtilities.cs)

## Verification limits

The cloud checks compile/test the shared C# core and parse every Unity source. They do not execute Unity Package Manager, compile against Unity assemblies or validate rendering/device builds. Unity Editor is installed on the user's laptop, not in this cloud environment. Editor import, actual package resolution, play mode and Android validation remain pending on the laptop; the migration does not claim these checks passed.
