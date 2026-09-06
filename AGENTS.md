# SpiralSquad Project Agent Guide

This is the living project constitution for SpiralSquad. Read it before changing gameplay, scenes, UI, project settings, packages, or build configuration. Keep it current whenever a setting, system boundary, target platform, or development milestone changes.

The repository also contains `.codex/AGENTS.md`, which has additional Unity-MCP workflow and collaboration rules. User instructions take priority over both files.

## 1. Product mission

SpiralSquad is an original 3D crowd-runner: the player auto-runs forward, steers a growing squad through math gates, collects currency, survives hazards and enemy mobs, and reaches a finish reward.

The intended direction is to clone and improve the *genre loop* of Crowd Master while keeping the implementation, assets, names, levels, and presentation original. Crowd Master is a reference for readable gate choices, short runner levels, crowd growth/loss, and satisfying encounters; do not copy protected assets, exact levels, UI, branding, or presentation.

SpiralSquad's identity is the Fermat/golden-angle spiral crowd formation. Preserve that identity as the crowd system improves instead of replacing it with a generic grid or blob.

Primary output target: a small, responsive, desktop/mobile-friendly WebGL build that runs in a browser without native-only dependencies. Treat browser size, memory, CPU, GPU, input, loading, and download size as product requirements from the beginning.

## 2. Current verified baseline

Baseline verified on 2026-09-03. Re-check these values after Unity upgrades, package changes, or build-target changes.

| Area | Current value | Source of truth |
|---|---|---|
| Unity editor | `6000.3.16f1` revision `a56f230f6470` | `ProjectSettings/ProjectVersion.txt` |
| Product name | `To the Top` | `ProjectSettings/ProjectSettings.asset` |
| Color space | Linear | `ProjectSettings/ProjectSettings.asset` / live PlayerSettings probe |
| Render pipeline | Universal Render Pipeline | `ProjectSettings/GraphicsSettings.asset` |
| URP package | `17.3.0` | `Packages/manifest.json` |
| Active editor target | WebGL | live Unity Editor probe |
| WebGL compression | Brotli | live Unity Editor probe / `ProjectSettings/ProjectSettings.asset` |
| WebGL memory | 32 MB | live Unity Editor probe / `ProjectSettings/ProjectSettings.asset` |
| WebGL threads | Disabled | live Unity Editor probe |
| WebGL decompression fallback | Disabled | live Unity Editor probe |
| WebGL exception support | Explicitly thrown exceptions only | live Unity Editor probe |
| WebGL hashed filenames | Disabled | live Unity Editor probe / `ProjectSettings/ProjectSettings.asset` |
| WebGL quality index | `0` (Mobile profile) | `ProjectSettings/QualitySettings.asset` |
| Default web resolution | 960 x 600 | `ProjectSettings/ProjectSettings.asset` |
| Build scenes | Only `Assets/Scenes/SampleScene.unity` is enabled | `ProjectSettings/EditorBuildSettings.asset` |
| Working scene | `Assets/Scenes/Level4.unity` is the current implementation scene; `Level3.unity` remains the previous implementation/workbench, `Level2.unity` remains the earlier implementation/workbench, and `Level1.unity` remains the reference/workbench scene | Unity Editor scene state, verified 2026-09-06 |
| Level 2 status | `Assets/Scenes/Level2.unity` is saved with Momentum Trapworks UI, hidden one-shot falling traps, and a 6-10m/s distance speed ramp; it is not yet enabled in Build Settings pending scene-order approval | Unity scene/script verification and `ProjectSettings/EditorBuildSettings.asset` |
| Level 3 status | `Assets/Scenes/Level3.unity` is saved with Pulsebound Foundry UI, a 340m track, pulse plates, 3-second spike shuttles, swinging hammers, and pooled side cannons; it is not yet enabled in Build Settings pending scene-order approval | Unity scene/script verification and `ProjectSettings/EditorBuildSettings.asset` |
| Level 4 status | `Assets/Scenes/Level4.unity` is saved with Vaultline Citadel UI, a 360m track, jump-state support, prefab-backed vault barriers/rising blocks/sweep beams/shutter blocks, and limited pooled side cannons; it is not yet enabled in Build Settings pending scene-order approval | `Assets/Scenes/Level4.unity`, `Assets/Scripts/PlayerController.cs`, `Assets/Scripts/JumpBarrierHazard.cs`, `Assets/Scripts/RisingBlockHazard.cs`, `Assets/Scripts/SweepBeamHazard.cs`, `Assets/Scripts/ShutterBlockHazard.cs`, verified 2026-09-06 |
| Level 4 crowd/trap rule | Level 4 does not show prefab `Telegraph` children; barriers, rising blocks, sweep beams, and shutters resolve immediately from visual-runner overlap. Each destroyed visual clone removes the rounded whole-number average of `logicalCount / visibleCloneCount`, recalculating before each subsequent loss. The blue badge shows logical count; `VisualRunnerCount` remains bounded | `Assets/Scripts/PlayerCrowdManager.cs`, `Assets/Scripts/Level4HazardBase.cs`, and Level 4 hazard scripts; implementation pending Unity compile/playtest |
| Input | Unity Input System; mouse drag and keyboard fallback are implemented | `Packages/manifest.json`, `Assets/InputSystem_Actions.inputactions`, `Assets/Scripts/PlayerController.cs` |
| Assembly definitions | None found under `Assets/**/*.asmdef` | repository scan on baseline date |

Important scene rule: `Level4` is the current implementation scene, `Level3` is the previous implementation/workbench, `Level2` is the earlier implementation/workbench, `Level1` is the gameplay/UI reference workbench, and only `SampleScene` is currently enabled in Build Settings. Do not assume editor playability means build inclusion. Before a WebGL milestone, explicitly choose the scene order and update Build Settings and this file together.

## 3. Repository structure

```text
Assets/
  Art/                         Project materials, meshes, and custom UI art
  Editor/                      Editor-only builders and tooling
  InputSystem_Actions.inputactions
  Materials/                   Player and enemy materials
  Models/                      Character and object models
  Plugins/NuGet/               Third-party managed plugins/DLLs
  Prefabs/                     Door, enemy, player, and UI prefabs
  Resources/UI/                Runtime-loaded CounterBadge prefab
  Scenes/
    Level1.unity               Reference/workbench level: multiplier ramp-up/sawmill
    Level2.unity               Implemented Spiral Trapworks level; 300m track and planned trap sections
    Level3.unity               Implemented Pulsebound Foundry level; 340m track and timed pulse/spike/hammer/cannon sections
    SampleScene.unity          Current enabled build scene; legacy/sample flow
  Scripts/                     Gameplay, UI, effects, and pooling MonoBehaviours
  UI/kenney_ui-pack/           Supplied UI pack, fonts, sprites, sounds, previews, license
  _Recovery/                   Recovery scene snapshots; do not use as production content

Packages/                      Package manifest and lock file
ProjectSettings/               Unity/player/quality/graphics/build settings
.codex/AGENTS.md                Existing MCP workflow and detailed gameplay notes
```

Current gameplay scripts are intentionally in one flat folder. Keep the existing files stable while the prototype is being proven. If the project grows, introduce folders by responsibility rather than moving files opportunistically:

```text
Assets/Scripts/
  Bootstrap/       scene composition, run state, service wiring
  Crowd/           player crowd, runner representation, formation, combat
  Track/           gates, doors, finish, hazards, pickups, spawners
  Economy/         currency, progression, shop data
  UI/              HUD, menus, result screens, presentation adapters
  Platform/        WebGL/browser integration and safe platform abstractions
  Shared/          small reusable utilities and pooling
```

Do not create this future layout as a broad refactor until the next feature actually needs a boundary. Preserve Unity script GUIDs and scene references when moving files.

## 4. Current scene and system map

`Assets/Scenes/Level1.unity` remains the reference/workbench scene and contains the original authored areas:

- `Level 1 - Multiplier Ramp-Up`: parent for `Systems`, `Track`, `Gates`, `Pickups`, `Lighting`, `Section 2 - The Sawmill Bottleneck`, and `FinishGate`.
- `Player`: player controller, crowd manager, runner prefab/pool, world level label, and world-space count badge.
- `Level 1 Camera`: main camera.
- `CM Level1 Crowd Follow`: Cinemachine follow camera support.
- `EventSystem`.
- `Level HUD Canvas`: current HUD, progress bar, stage markers, currency, settings, shop, run labels, and finish screen.

Responsibility map:

- `PlayerController.cs`: `CharacterController` movement, forward speed, horizontal mouse/touch drag, keyboard fallback, jump, gravity, and game-state movement lock.
- `PlayerCrowdManager.cs`: logical runner count, visual runner pool, Fermat spiral placement, compressed visual representation, edge-falling, combat participation, and game-over checks. Current safety limits include a pool cap of 500 and a visual clone cap tied to that limit.
- `Gate.cs` and `Door.cs`: additive/multiplicative gate choices and one-time trigger behavior.
- `Enemy.cs`, `EnemySpawner.cs`, `GroupSpawner.cs`: enemy state/movement, group placement, detection, and crowd combat.
- `CircularSaw.cs`, `ConeHazard.cs`, `FallingHazard.cs`: track hazards.
- `ObjectPool.cs`, `ProjectileLauncher.cs`, `TrackingProjectile.cs`: allocation-conscious projectile reuse.
- `DeathPopEffect.cs`: runner-loss presentation effect.
- `CurrencyWallet.cs`: run currency plus wallet currency, exposed through a singleton. `LevelHud` currently displays run currency.
- `CountBadge.cs` and `Resources/UI/CounterBadge.prefab`: world-space player/enemy count badges. `PlayerCrowdManager.Start()` attaches the blue player badge using `ActiveRunnerCount`; `CountBadge` loads the visual prefab from `Resources`.
- `LevelHud.cs`: current Level1 HUD currency/progress updates, settings/shop panels, sound toggle, pause state, restart, and menu exit.
- `UIManager.cs`: older/global menu, HUD, game-over, win, progress, and Cinemachine fall-camera flow. It remains for compatibility with the sample/legacy flow.
- `FinishGate.cs`: detects completion and currently builds a finish overlay dynamically.

There are two UI flows (`LevelHud` and `UIManager`). Treat `LevelHud` as the owner of the current Level1 HUD. Do not add a third UI authority. When migrating the legacy flow, move behavior deliberately and remove duplicate ownership only after both scenes and build flow are verified.

`Assets/Scenes/Level2.unity` reuses the verified Level1 systems/UI/camera setup and adds the authored `Trap Sections` hierarchy: `Cone Weave`, `Falling Tutorial`, `Gate Combination`, `Saw Relay`, `Crossfire`, and `Final Gauntlet`. Its current scene-level settings are a 300m track, `LEVEL 2 - MOMENTUM TRAPWORKS` HUD copy, hidden one-shot falling traps with 7m forward activation offsets, and a 6-10m/s player speed ramp from z=3 to z=297. Gate B/C currently use integer x2 multipliers because `Gate.value` is an integer; implementing the plan's x2.5 values requires an explicit gameplay-math change.

Level 4 hazards have no visible activation indicator. They process visual-runner overlap immediately; they do not wait for a warning telegraph or remove an arbitrary percentage of the crowd.

## 5. Development direction

### Core loop

1. Start with a clear, short forward-running level.
2. Present an obvious left/right gate decision (`+N` or `xN`).
3. Reward good choices with visible spiral crowd growth and currency.
4. Telegraphed hazards test steering and crowd width.
5. Enemy mobs create readable risk and controlled runner loss.
6. Offer recovery or reward moments before the finish.
7. Finish with a strong count/currency result and a clear next action.

The improvement target is not simply more objects. Improve decision quality, crowd readability, feedback, pacing, balance, replayability, and browser performance.

### Architecture direction

Use thin MonoBehaviours at Unity boundaries and keep rules testable where practical:

- Plain C# is preferred for deterministic math, gate evaluation, score calculation, and balancing rules.
- ScriptableObjects are the preferred home for authored level, gate, hazard, crowd, economy, and presentation configuration as soon as values are shared across scenes or need balancing outside code.
- Scene objects should own references to the systems they require. Avoid repeated global searches and new singletons.
- Use events or explicit interfaces for cross-system notifications; do not let UI poll every gameplay detail or let hazards directly manipulate unrelated UI.
- Keep `CurrencyWallet` and `UIManager` compatibility stable until a planned migration replaces their global access.
- Avoid adding networking, online services, or server simulation to the single-player WebGL core unless the product scope explicitly changes.

### Crowd quality bar

- Keep logical count separate from visual count.
- `PlayerCrowdManager.ActiveRunnerCount` is the logical count shown by the blue badge; `VisualRunnerCount` is the bounded active visual clone count. Recalculate the rounded logical-per-visual average before each visual clone is removed.
- Retain the spiral formation and make its density, spacing, and compression readable at a glance.
- Keep visual clones bounded; use pooling and predictable reuse.
- Avoid per-runner expensive physics. Prefer aggregate checks, cached arrays, spatial partitioning, and deterministic crowd rules.
- Gate and hazard math must be deterministic and testable without a scene when feasible.

## 6. WebGL/browser rules

WebGL is the release constraint, not a final port. New runtime code must be browser-safe:

- Prefer the Input System with mouse, touch, and keyboard mappings. Do not require a native controller, filesystem, process, thread, or platform-specific API for the main loop.
- Avoid gameplay-time `Instantiate`/`Destroy`, unbounded lists, LINQ in hot paths, per-frame string formatting, and repeated `Find*` calls. Pool high-frequency objects and cache references.
- Keep renderers, materials, lights, shadows, particle counts, post-processing, and shader variants conservative for mobile browsers.
- Keep assets small: reuse materials, atlas UI sprites where useful, compress textures appropriately, and avoid unnecessary animation/model variants.
- Keep loading predictable. Do not add synchronous disk/network work, large `Resources` scans, or runtime-only dependencies without a measured reason. Plan to replace `Resources` with explicit references or Addressables only if the size/loading evidence justifies it.
- Never enable WebGL threads, decompression fallback, or a larger memory heap just because a build fails. First identify the asset/code cause; change the setting with a documented measurement and browser compatibility check.
- Validate the actual WebGL build in at least one Chromium-based browser and one lower-powered/mobile-like profile before calling a performance task complete.

Initial performance budgets to track (targets, not yet measured):

- stable 60 FPS on a desktop browser; graceful 30 FPS on a lower-powered browser profile;
- no recurring gameplay GC spikes from crowd movement;
- bounded active visual runners and pooled hazards/projectiles;
- first playable interaction after the smallest practical download;
- no console errors, missing assets, or input dead zones in the browser build.

Record measured budgets in this file when profiling establishes them. Do not present targets as achieved results.

## 7. Configuration ownership and change protocol

Use the narrowest owner for every setting:

| Setting type | Owner |
|---|---|
| Player speed, gate value, hazard timing, crowd limits | serialized component now; ScriptableObject when shared/balanced centrally |
| Scene hierarchy and references | scene/prefab asset |
| HUD visual styling | `Assets/Scenes/Level1.unity` plus supplied assets in `Assets/UI/` or `Assets/Art/UI/` |
| Input actions | `Assets/InputSystem_Actions.inputactions` and the consuming controller |
| Package/version choice | `Packages/manifest.json` and `Packages/packages-lock.json` |
| Rendering/player/build/quality | `ProjectSettings/` files; change through Unity when possible |
| Browser-specific behavior | `Assets/Scripts/Platform/` or a small explicit adapter, not scattered preprocessor branches |

When adding or changing a setting:

1. Give it a descriptive name, a safe default, an owner, and a reason.
2. Clamp designer-facing values with `OnValidate` when they can create frame spikes or huge scenes.
3. Decide whether it is per-level, per-platform, or global before placing it.
4. Update the relevant source asset and this `AGENTS.md` baseline if it changes a documented current value.
5. Verify the narrowest useful path: compile, playtest, screenshot, profiler capture, or WebGL build.
6. Note any unverified assumption as `TBD` instead of inventing a value.

## 8. Milestones

- **M0 — Prototype truth:** choose the production start scene, reconcile `SampleScene`/`Level1` Build Settings, remove duplicate UI ambiguity, and keep the current loop playable.
- **M1 — Data and balance:** move shared gate, hazard, crowd, level, and economy values into authored data; add focused tests for gate math and crowd-count rules.
- **M2 — Crowd feel:** improve spiral readability, formation transitions, crowd compression, loss feedback, combat fairness, and recovery pacing.
- **M3 — Content:** add original gate/hazard/enemy patterns with deliberate risk/reward pacing and reusable level sections.
- **M4 — UI/presentation:** consolidate UI ownership, reuse the supplied `Assets/UI/kenney_ui-pack`, preserve responsive layout, and make all feedback readable on browser/mobile aspect ratios.
- **M5 — WebGL hardening:** profile allocations/rendering/loading, confirm browser input, tune quality tiers, reduce payload, and run a clean WebGL build.
- **M6 — Release readiness:** product naming/branding, licensing audit, build scene audit, browser matrix, save/economy audit, and final error-free playthrough.

## 9. Verification checklist

Before merging a gameplay change:

- Unity compiles with no new errors or warnings that affect the changed path.
- The relevant scene opens and plays from a clean state.
- Gate math, runner count, finish state, currency, pause state, and restart behavior are checked when touched.
- New allocations are justified and bounded.
- UI remains readable at the current 960x600 web reference and a narrow/mobile-like aspect ratio.
- A WebGL build is tested when the change touches input, loading, assets, rendering, memory, packages, or platform APIs.
- Scene, package, and ProjectSettings changes are intentional and included in the handoff.

## 10. Keeping this file correct

This document is not a substitute for the Unity assets or code. If it disagrees with the repository, verify the repository and update this file. Every new project-wide setting, architecture decision, platform constraint, or milestone completion should be added here with its source path and verification date. Keep statements labeled as current, target, or TBD so future agents can distinguish facts from direction.

Error tracking rule: whenever an error is identified and fixed, append a concise entry to [ERROR_TRACKING.md](ERROR_TRACKING.md) summarizing the error, its verified or best-known cause, and the fix. Update the entry when later investigation changes the cause or fix. Do not present an unverified cause as fact; label it as TBD or suspected.

## 11. Implementation plan references

The proposed Level 3 plan is:

- [LEVEL3_IMPLEMENTATION_PLAN.md](LEVEL3_IMPLEMENTATION_PLAN.md): Level 3 "Pulsebound Foundry" layout, new pulse/spike/hammer/cannon trap set, exact 3-second spike endpoint travel, difficulty progression, scene construction order, WebGL constraints, and playtesting gates.

The proposed Level 4 plan is:

- [LEVEL4_IMPLEMENTATION_PLAN.md](LEVEL4_IMPLEMENTATION_PLAN.md): Level 4 "Vaultline Citadel" layout, jump-over barriers and map blocks, reusable obstacle prefabs, new vertical trap combinations, difficulty progression, scene construction order, WebGL constraints, and playtesting gates.

Before implementing any new level or major gameplay feature, read the relevant design plan and use it as the implementation boundary. The current Level 2 plan is:

- [LEVEL2_IMPLEMENTATION_PLAN.md](LEVEL2_IMPLEMENTATION_PLAN.md): Level 2 “Spiral Trapworks” layout, coordinates, gate values, trap combinations, difficulty progression, scene construction order, WebGL constraints, and playtesting gates.

For Level 2 work, do not invent replacement coordinates or trap pacing without first updating the plan or documenting the approved design change. For future levels and features, create or update a linked Markdown plan before implementation when the work introduces new scene structure, gameplay pacing, coordinate layouts, settings, or platform constraints.

Every implementation request should be checked against:

1. The applicable plan in this section.
2. The verified baseline and ownership rules in this file.
3. The WebGL/browser constraints in this file.
4. The verification checklist before the change is considered complete.
