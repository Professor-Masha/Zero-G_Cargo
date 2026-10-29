# Fifty-sector campaign

The runtime source of truth is `Assets/_Project/Resources/Levels/campaign.json`. Legacy three-level ScriptableObject assets/fixtures remain only for the old prototype regression checks; runtime progression now loads the fifty-sector JSON.

| Sectors | Cargo | New demands |
|---|---:|---|
| 1–2 | 1 | Short direct routes and docking; no timer |
| 3–10 | 1 | Routes around a growing meteorite; two failures reveal a brief guide |
| 11–20 | 2 | Timer, colour separation and repulsor barriers |
| 21–30 | 2 | Moving meteorite plus tighter per-delivery budgets |
| 31–34 | 3 | Additional delivery route and gravity-field influence |
| 35–40 | 3 | Ordered delivery dependencies; smaller locked bays |
| 41–50 | 3 | Additional energy-field boundary and tightest authored constraints |

Every successive sector increases authored drift. After sector 11 the allowed time per delivery decreases, including the tier transition to three deliveries. Meteorite size/field strengths and path spans also change within tiers. Layouts include mirrored variations of route families; they are not fifty independently human-playtested handcrafted puzzles.

The automated suite solves every bundled guide with the actual flight, collision, docking, time and dependency code. It found and corrected a four-level wrong-bay capture conflict during development. Simulation solvability is necessary but does not prove touch usability or fun, or a median ≤3-attempt solve rate.

Engagement comes from earned stars, visible unlocks, learnable momentum, new mechanics introduced by tier, recoverable collisions and immediate retries. There is no energy meter, compulsory ad, purchase pressure or online dependency. Human playtesting should measure first-attempt understanding, retry friction, completion times and whether adjacent sectors feel meaningfully harder. Adjust layouts using Campaign editor and rerun all route tests after changes.
