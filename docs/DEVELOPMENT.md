# Prototype scope and architecture

The supplied documents describe the intended full Android release and an approximately 28-day implementation plan. This initial commit establishes a reviewable core slice rather than claiming that release is complete. Embedded master-prompt examples and confirmation templates remain reference material; the user's request authorized starting development and pushing the result.

## Implemented in source

- Unity 6.6 (6000.6.7f1) project, pinned direct packages, stable asset GUIDs and four registered scenes.
- ScriptableObject configuration with the specified inertia defaults: acceleration 8, speed 6, drag 0.4, braking 12, drift 0.15, maximum route length 6.
- Single-pointer touch/mouse drawing with pickup radius, UI rejection, sample spacing, fixed storage, route budget clipping and one completion event. LineRenderer corner rounding supplies visual smoothing; authored physics uses the sampled polyline.
- Engine-independent manual inertia at 60 fixed steps/second. Lookahead preserves velocity through turns. Free-flight routes drift beyond their endpoint; routes ending within a bay's capture radius engage a steering/braking controller for safe arrival. No Rigidbody2D movement.
- Pure colour/speed docking rules and one matched bay per tutorial. Docking eases over 0.3 seconds. Meteorites and station boundaries trigger a 1.2-second spin/return. Swept circle queries catch meteorite tunnelling.
- Three authored, untimed early-tier levels, each with one shuttle, one bay and one meteorite. Their guide routes are tested against the actual simulation and collision radii. Two collisions reveal a three-second ghost route.
- Prebuilt TMP main menu, level select/locks/stars, gameplay, pause, win, sound settings and bundled prototype privacy screen. Escape/Android back navigates; quitting from menu requires a second back press. App pause/focus loss freezes the level and saves.
- Reusable station objects, sprite textures, line buffers and prebuilt UI. No object creation during flight; only transition-time labels allocate. Full-device allocation claims require the Unity Profiler.
- Schema-v1 JSON with 50 star slots, best-star preservation, progress unlocking, sound preferences, atomic file replacement and backup recovery. Unknown future schemas are read-only to avoid silently downgrading saves.
- Two original synthesized placeholder clips: docking chime and collision boing.

The persistent `GameManager` composes services. `PathDrawer.OnPathComplete` feeds `LevelManager`; its collision/dock/completion events update audio, UI and save progress. All subscriptions are removed on destruction. The small prototype uses plain service instances instead of five empty manager singletons; SDK managers will be added when those integrations become real. Serializable save DTO fields are public for the JSON codec; Unity inspector wiring uses private serialized fields.

## Not implemented or not verified

The full 50-level game, multiple simultaneous shuttles, barriers, wells, chain deliveries, timer/fail-screen flow, level-authoring editor, particles, full ambient audio/music/haptics, reset-save UI, animation polish and final fonts/art are future work. Early-level star ratings currently use collision count; later timed ratings require the timer milestone. No fake SDK adapters, ad buttons, Firebase calls or production identifiers are included.

The first import prepares URP/TMP and Android settings on the laptop. Unity package resolution/lock generation, Unity API compilation, scene rendering, touch input, lifecycle behavior and Android builds require local verification. This environment can test the pure core and source syntax only. A 60-FPS target is configured; no device performance or zero-allocation claim is made.

The original DOCX specifications retain their historical Unity 2022.3 requirements. The current editor/package target is documented in [the Unity 6.6 migration](UNITY_6_UPGRADE.md).

## Next milestones

1. Open in Unity on the laptop, run preparation, clear any import/compiler issues, complete all three levels with mouse/touch and tune inertia with human feedback. Commit generated Unity settings/assets and `Packages/packages-lock.json` after successful package resolution.
2. Expand level data and pooling for multiple cargo/bay pairs; add barriers/wells and their force/collision tests, then the editor validation tool.
3. Add timed win/fail flow, full settings/lifecycle polish and remaining soundscape. Failure must offer Try Again and Level Select only, with no ads.
4. Author and playtest the remaining levels. Simulation-solvable guide routes do not establish the median-player ≤3-attempt target.
5. Integrate AdMob test ads and Firebase Analytics only, with offline fallbacks and real configuration supplied securely. Verify every ad-state rule and update privacy disclosures before distribution.
6. Profile on actual Android device tiers, build a signed AAB, and prepare store release. Git push is not publication to Google Play or deployment of a privacy-policy website.
