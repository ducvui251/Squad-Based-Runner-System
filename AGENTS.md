# SpiralSquad Project Agent Guide

This file defines project intent, implementation boundaries, collaboration preferences, and verification requirements. Read it before changing gameplay, scenes, UI, packages, or build configuration. Also read [.codex/AGENTS.md](.codex/AGENTS.md) for supplemental Unity-MCP workflow guidance. Explicit user instructions take priority over project guidance. When documentation disagrees with source assets or live state, investigate and record the discrepancy instead of silently treating either as current.

## 1. Collaboration and model routing

Only use Astra when the user explicitly requests Astra for the task. Complexity, ambiguity, importance, available quota, or an orchestration preset does not authorize automatic escalation. Authorization persists for that task until changed, not for unrelated future tasks. Otherwise continue with the current model or available Sol/Luna routing. These are role preferences; never claim to switch models unless the environment actually supports and performs the switch.

| Model | Preferred role | Best fit |
|---|---|---|
| Astra, only when requested | Architect / lead engineer | Ambiguous diagnosis, repository investigation, architecture, risk analysis, specifications, and highly interconnected implementation |
| Sol | Senior implementation engineer | Difficult but well-specified execution, integration, meaningful tests, ordinary debugging, and iteration |
| Luna | Fast execution worker | Bounded small fixes, repetitive implementation, mechanical edits, focused tests, and cleanup |

Choose by unresolved reasoning and integration risk, not code volume. When requested, Astra may both plan and implement; completing a plan is not a reason by itself to switch. Preserve continuity on important, interconnected work when the user's usage budget permits. Orchestration is useful for cost, speed, and independent parallel work; handoffs are optional.

Before a substantial handoff, leave a concrete specification in the relevant implementation plan with the objective, repository state, evidence, decisions and rationale, rejected alternatives, risks, assumptions, exact files to touch, dependencies, ownership, "do not change" items, acceptance criteria, required checks, completed work, and remaining work. The receiving model must verify the repository against that handoff and surface contradictory evidence before changing architectural decisions. Parallel tasks need explicit file ownership; keep tightly coupled changes under one owner. Newly discovered uncertainty does not authorize invoking Astra.

Work autonomously within the requested scope. Use existing authorization and make routine implementation decisions without repeated permission requests. Ask when missing intent materially changes the result or when a destructive or out-of-scope action requires new authority. A diagnosis/review requests evidence and explanation; an implementation/fix requests changes and verification. Treat `--dry-route` as a routing preview: describe the selected roles and intended actions without executing fixes, builds, play sessions, or other mutations.

Preserve user edits in a dirty worktree. Do not revert unrelated files, broadly refactor, delete user assets, change build order, or install dependencies as incidental cleanup. Report the concrete result, relevant verification, and remaining limitations concisely.

## 2. Product mission

SpiralSquad is an original 3D crowd-runner: auto-run forward, steer a growing squad through math gates, collect currency, survive hazards and enemy mobs, and reach a finish reward. Crowd Master is a genre reference for readable choices, short levels, crowd growth/loss, and satisfying encounters. Keep implementation, assets, names, levels, UI, branding, and presentation original.

The Fermat/golden-angle spiral formation is the project's defining mechanic. Preserve its identity while improving density, spacing, compression, feedback, and performance.

The primary release target is a small, responsive WebGL build for desktop and mobile browsers. Download size, startup time, memory, CPU/GPU cost, touch input, and narrow layouts are product constraints from the start.

## 3. Baseline and evidence

Repository settings below were inspected on 2026-09-09. Live editor observations and earlier playtests are separate evidence; do not imply that a source inspection is a fresh runtime or browser test. Recheck affected values after editor, package, profile, or platform changes.

| Area | Repository value | Source |
|---|---|---|
| Unity | `6000.3.16f1`, revision `a56f230f6470` | `ProjectSettings/ProjectVersion.txt` |
| Product name | `To the Top` | `ProjectSettings/ProjectSettings.asset` |
| Rendering | Linear color space; URP assigned | `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/GraphicsSettings.asset` |
| URP / Input System / Cinemachine | `17.3.0` / `1.19.0` / `3.1.6` | `Packages/manifest.json` |
| Web reference resolution | 960 x 600 | `defaultScreenWidthWeb`, `defaultScreenHeightWeb` in PlayerSettings asset |
| WebGL quality default | Index `0` | `ProjectSettings/QualitySettings.asset` |
| WebGL memory | Initial 32 MB; maximum 2048 MB; serialized growth mode `2` | `webGLInitialMemorySize`, `webGLMaximumMemorySize`, `webGLMemoryGrowthMode` in PlayerSettings asset |
| WebGL threads / decompression fallback / hashed filenames | Disabled | PlayerSettings asset |
| WebGL compression / exceptions | Brotli / explicitly thrown exceptions only, as previously probed; serialized values remain `0` / `1` | PlayerSettings asset and earlier live probe |
| Saved shared build scene list | Only `Assets/Scenes/SampleScene.unity` enabled | `ProjectSettings/EditorBuildSettings.asset` |
| Level 5 legacy stopper layer | `PlayerOnly`, index `6`; retained for disabled compatibility children | `ProjectSettings/TagManager.asset`, `CrackedSpanHazard.cs` |
| Gameplay assembly layout | No `.asmdef` found under `Assets` in this review | Asset file inventory |

32 MB is initial memory, not a measured peak or total runtime cap. The 2048 MB configured maximum is not a performance target or evidence that a device can sustain that usage. Actual browser memory and frame-time measurements remain TBD.

Level5 is the current implementation/workbench scene; Level4, Level3, and Level2 are earlier workbenches, and Level1 is the original gameplay/UI reference. The earlier live target was WebGL; query current editor state before platform operations.

Build inclusion needs both saved and live verification: an earlier live probe returned `Level5.buildIndex = 1`, while the inspected shared build settings list only SampleScene. The reason for that discrepancy is TBD; inspect the active Build Profile and its scene list before claiming a production launch order. Do not overwrite the user's live configuration to match the document. Choose and document the intended production scene order before a WebGL milestone.

## 4. Repository and ownership

| Location | Responsibility |
|---|---|
| `Assets/Scripts/` | Gameplay, crowd, UI, effects, and pooling; currently a flat folder |
| `Assets/Editor/` | Editor-only scene builders and tooling, including Level 3–5 builders |
| `Assets/Scenes/` | SampleScene and Level1–Level5 authored scenes |
| `Assets/Prefabs/` | Player, enemy, door, UI, and reusable trap prefabs |
| `Assets/Art/`, `Assets/Materials/`, `Assets/Models/` | Presentation assets and models |
| `Assets/UI/kenney_ui-pack/` | Supplied UI sprites, fonts, sounds, previews, and license |
| `Assets/Resources/UI/` | Runtime-loaded CounterBadge prefab |
| `Assets/InputSystem_Actions.inputactions` | Authored input actions |
| `Assets/Plugins/NuGet/` | Third-party managed plugins; check platform compatibility when touched |
| `Assets/_Recovery/` | Recovery snapshots, not production content |
| `Packages/` | Package manifest and lock file |
| `ProjectSettings/` | Player, rendering, quality, physics layers, and shared build settings |

Keep the flat gameplay layout stable until a feature needs a responsibility boundary. Potential future folders are Bootstrap, Crowd, Track, Economy, UI, Platform, and Shared; this is not authorization for a broad move. Preserve script GUIDs, asset `.meta` files, prefab links, and serialized references.

Use the narrowest owner: per-level values belong to serialized components or authored configuration; reusable shared balance data may warrant ScriptableObjects; object wiring belongs to scenes/prefabs; input belongs to actions and their controller; packages and platform settings belong to their source assets. Update builders when changes to generated content must survive regeneration.

## 5. Systems and invariants

| System | Owner |
|---|---|
| CharacterController movement, steering, speed ramp, jump, gravity, movement locks | `PlayerController.cs` |
| Logical crowd count, bounded visual pool, spiral placement, edge losses, combat, game-over checks | `PlayerCrowdManager.cs` |
| One-shot additive/multiplicative gates | `Gate.cs`, `Door.cs` |
| Enemy state, placement, group detection, and crowd combat | `Enemy.cs`, `EnemySpawner.cs`, `GroupSpawner.cs` |
| Track hazards | Saw/cone/falling/pulse/spike/hammer scripts; Level 4 hazard family; `CrackedSpanHazard.cs` |
| Reused projectiles | `ObjectPool.cs`, `ProjectileLauncher.cs`, `TrackingProjectile.cs` |
| Runner-loss presentation | `DeathPopEffect.cs` |
| Run and wallet currency | `CurrencyWallet.cs`, with pickup behavior in `DollarCoin.cs` |
| World count badges | `CountBadge.cs`, `Resources/UI/CounterBadge.prefab` |
| Authored level HUD, settings/shop, pause, restart, exit | `LevelHud.cs` |
| Legacy menu/HUD/results and fall-camera compatibility | `UIManager.cs` |
| Finish detection and dynamically built finish overlay | `FinishGate.cs` |

### Crowd and movement

Keep logical count separate from visual count. `ActiveRunnerCount` is the logical count shown by the blue badge; `VisualRunnerCount` is the bounded active clone count. The previously documented pool safety cap is 500; inspect current serialized overrides and pool code before balancing or changing it.

For visual-clone hazard losses, use the crowd manager's removal API and recalculate the rounded whole-number `logicalCount / visibleCloneCount` value before each removal. Do not directly mutate both counts independently. Preserve the spiral and pool reuse; avoid expensive physics on every clone.

Do not freeze a positive logical crowd solely because a terminal flag remains latched. The current PlayerController guard checks both `IsGameOver` and an empty logical count; this is a local protection, not proof that every game-state consumer handles stale state. Check hazards, completion, restart, and UI together when changing that boundary.

### UI and game state

Two UI flows currently coexist. `LevelHud` owns the authored level HUD; `UIManager` supports the legacy/sample flow. Preserve compatibility and do not add a third authority. Consolidation is planned work, not incidental cleanup.

Standalone levels use `LevelHud.Awake/OnDestroy` and `UIManager.SetExternalGameActive` to register activity when no legacy UIManager instance exists. Do not add a UIManager merely to activate a standalone level. Verify fresh start, pause/resume, loss, win, restart, and scene transitions when changing this bridge.

### Hazards

Hazards must be visually readable and respect reaction time, crowd width, and speed. Telegraph behavior is level-specific: Level 4 barriers, rising blocks, sweep beams, and shutters hide prefab `Telegraph` children and resolve visual-runner overlap immediately, with airborne-clearance protection. Do not reintroduce warnings or arbitrary percentage losses into that family without a design change.

Level 5 jump clarification: `CrackedSpanHazard` starts a bounded crossing session before each gap and evaluates the lead with the real `PlayerController` trajectory. `PlayerCrowdManager` then classifies non-lead visual runners by identity and actual world-space Z as near-side, over-gap, far-side, or failed; a successful lead crossing does not guarantee that trailing runners survive.

Level 5 fractures are a separate contract: the current implementation replaces the continuous road with solid segments at 0–304, 308–386, and 390–420, leaving real 4.0m gaps at z=304–308 and z=386–390. `CrackedSpanHazard` evaluates the lead at the center and far edge, then keeps processing the bounded crowd session after lead success. `PlayerCrowdManager` uses each runner's actual world-space Z, the shared lead vertical trajectory, the authored road surface, and far-edge clearance to latch SAFE_FAR_SIDE or FAILED; only FAILED runners enter the pooled gravity-fall path, preserving the rounded logical-per-visual removal contract. The crossing scan iterates its stable identity map so compression/reformation cannot invalidate the active-list traversal. Legacy `Bridge` and `LeadStopper` children remain disabled compatibility objects; they are not gameplay surfaces. The lead-fall terminal state stops forward movement while the crowd manager continues falling-runner cleanup. Exact full/partial balance under compressed representation still requires full route and browser testing.

`ConeHazard` edge elimination must remain limited to the cone's local Z window; a cone must not kill distant off-edge runners elsewhere on the track.

## 6. Level plans and current content

Read the relevant plan before level implementation. Plans define design intent; saved assets and runtime checks establish what is implemented. Document deviations rather than silently replacing coordinates, gate math, or trap pacing.

| Scene | Content summary | Design reference |
|---|---|---|
| Level1 | Original multiplier ramp-up/sawmill reference, authored HUD and camera setup | Existing scene and `Assets/Editor/Level1Section2Builder.cs` |
| Level2 | 300m Momentum Trapworks; cone weave, hidden one-shot falling traps, saw relay, crossfire, final gauntlet; 6–10m/s ramp | [LEVEL2_IMPLEMENTATION_PLAN.md](LEVEL2_IMPLEMENTATION_PLAN.md) |
| Level3 | 340m Pulsebound Foundry; pulse plates, three-second spike endpoint travel, swinging hammers, pooled side cannons | [LEVEL3_IMPLEMENTATION_PLAN.md](LEVEL3_IMPLEMENTATION_PLAN.md) |
| Level4 | 360m Vaultline Citadel; jump barriers, rising blocks, sweep beams, shutters, limited pooled cannons | [LEVEL4_IMPLEMENTATION_PLAN.md](LEVEL4_IMPLEMENTATION_PLAN.md) |
| Level5 | 420m Fracture Relay; recombined Level 2–4 hazards, two physical 4m road gaps with pooled split-crowd falls, 6–10m/s ramp | [LEVEL5_IMPLEMENTATION_PLAN.md](LEVEL5_IMPLEMENTATION_PLAN.md) |
| SampleScene | Legacy/sample flow; only scene in saved shared build list | Existing scene and build settings |

These summaries carry forward earlier implementation records; this documentation review is not a new full scene playthrough. On 2026-09-09, Level 5 received a clean compile/scene rebuild plus focused Play Mode probes for the physical gap, no-jump lead fall, pooled rear-runner fall, and lead-fall movement lock. Full clean playthrough, balance, browser performance, and release readiness remain unverified.

Level 2 falling traps were authored with 7m forward activation offsets and a speed ramp from z=3 to z=297. Gate B/C use integer x2 values because `Gate.value` is an integer; the plan's x2.5 values require a deliberate gameplay-math change.

Level 5 uses the Level4 systems/UI/camera setup and sections named Crosswind Pulse Weave, Drop-Saw Relay, Block-Saw Interlock, Pendulum Shutter Exchange, Fracture Tutorial, and Final Fracture Relay. Earlier authored coordinates place x2 gates at z=30/150/320 and cracked spans at z=304/386. Check the builder and saved scene before editing their layout.

For new levels or features introducing scene structure, pacing, coordinates, or platform constraints, create or update a linked implementation plan before implementation. Preserve forward Z travel, readable left/right lanes, descriptive hierarchy names, clear start/camera/finish placement, and trigger-only gates unless a physical blocker is intentional.

## 7. Implementation principles

Build the loop around clear gate choices, visible spiral growth, readable hazards, fair enemy encounters, recovery moments, and a useful finish reward. Improve decision quality and feedback before adding object count.

Use thin MonoBehaviours at Unity boundaries and deterministic plain C# for math and rules where practical. Add ScriptableObjects when shared configuration or authoring needs justify them. Keep references explicit and cached; avoid new singletons and repeated global searches. Prefer events or small interfaces at real system boundaries over hazards manipulating unrelated UI.

Keep designer values descriptive and safely clamped. Use `OnValidate` for safe validation; avoid operations that trigger unsafe lifecycle callbacks. Do not expand networking, online services, or server simulation into the single-player core without a scope change.

Reuse existing art and the supplied UI pack. Keep count badges legible, function colors distinct, and UI readable at 960x600 and narrow/mobile-like layouts. Follow existing architecture for local fixes; propose migrations explicitly when required.

## 8. WebGL requirements and performance targets

Runtime features must support browser-safe input and APIs. Prefer the Input System with mouse/touch and keyboard support. Do not require native filesystem/process/thread APIs for gameplay.

Bound visual clones, projectiles, effects, collections, and work per frame. Pool frequent objects and avoid recurring gameplay allocations, LINQ in hot loops, per-frame string formatting, repeated searches, and frequent Instantiate/Destroy. One-time UI construction is an existing behavior, not permission to allocate per frame.

Reuse materials, constrain shadows/lights/particles, keep shader variants and textures small, and justify new dependencies. Avoid broad Resources scans and synchronous loading stalls. Move from Resources to explicit references or Addressables only when loading/size evidence warrants it.

Do not increase memory settings, enable threads, or enable decompression fallback as an unexplained response to build failure. Identify the cause and document measurements and compatibility before changing platform settings.

Targets, not achieved measurements:

- Stable 60 FPS on desktop browsers and graceful 30 FPS on a lower-powered/mobile-like profile.
- No recurring crowd-movement GC spikes; bounded active visual objects and pooled hazards.
- Small first-playable download and predictable startup.
- No browser errors, missing assets, or input dead zones.

Record device/browser, build/profile, scene, crowd size, frame timing, allocations, memory, and loading measurements when available. A performance milestone requires an actual WebGL build tested in a Chromium-based browser and a lower-powered/mobile-like profile.

## 9. Unity workflow and verification

Inspect relevant assets, objects, components, scene dirtiness, and current editor state before mutation. Use available Unity MCP tools for scene/prefab work and Unity serialization for references. Discover exact object paths instead of guessing names. Preserve unsaved work; persist authorized scene edits deliberately and do not save incidental Play Mode changes.

Audit-only rule: when the user asks for a code audit, review, or inspection, use source/repository inspection and non-mutating checks only. Do not use Unity MCP tools to enter, control, or simulate Play Mode, alter scene state, or drive editor gameplay unless the user explicitly requests Play Mode control or a gameplay run. Read-only asset/scene inspection and compiler/log diagnostics remain allowed when they are within scope. If runtime evidence would help an audit, report it as a separate optional verification step and wait for explicit authorization.

After external asset/script edits, refresh Unity and inspect actual compilation results. A successful AssetDatabase refresh or an empty recent log window does not by itself prove that compilation ran, old warnings disappeared, or gameplay is correct. Separate project compiler/runtime failures from MCP connection errors and failed diagnostic snippets; use timestamps and stack traces to establish relevance.

Choose checks proportional to the changed behavior:

- Code: compiler feedback and focused rule/regression checks for the affected path.
- Gameplay: fresh start and relevant gate math, logical/visual losses, jumping, finish, currency, pause, restart, and transition behavior.
- Scene/UI: missing references, colliders, hierarchy, saved values, and scene/game screenshots at reference and narrow layouts.
- Platform-sensitive input, loading, assets, rendering, memory, packages, or APIs: an actual WebGL build and browser checks; report explicitly when unavailable.

Run existing meaningful tests where available. The earlier unfiltered EditMode invocation found no tests; do not report that as a passing suite. Do not create trivial tests merely to decorate a low-impact edit. Report scope honestly: source inspection, compilation, focused smoke test, full playthrough, and browser validation are different levels of evidence.

When an error is identified and fixed, append a concise entry to [ERROR_TRACKING.md](ERROR_TRACKING.md) with the symptom, verified or suspected cause, fix, and verification. Update it if later evidence changes the diagnosis. Record pending issues as pending; do not present hypotheses or untested fixes as established facts.

## 10. Milestones and document maintenance

| Milestone | Completion boundary |
|---|---|
| M0 — Prototype truth | Choose production start/order, reconcile shared/profile/live build configuration, clarify UI ownership, and verify the playable loop |
| M1 — Data and balance | Shared authored configuration and focused gate/crowd-count validation |
| M2 — Crowd feel | Readable formation changes, fair losses/combat, recovery pacing, and measured crowd costs |
| M3 — Content | Original reusable sections, deliberate risk/reward, and verified level pacing |
| M4 — UI/presentation | Deliberate UI consolidation and responsive readable feedback |
| M5 — WebGL hardening | Browser input, allocations/rendering/loading measurements, payload tuning, and a tested build |
| M6 — Release readiness | Naming/licensing, build order, browser matrix, save/economy audit, and error-free playthrough |

These are completion criteria, not claims that milestones have been achieved. Content existing in a workbench does not establish build inclusion or release readiness.

Keep durable constraints and ownership here; keep detailed coordinates/specifications in linked plans and defect history in ERROR_TRACKING.md. Update this guide when a documented value, boundary, platform constraint, user routing preference, or milestone status changes. Include source paths, verification dates, and explicit TBDs. Avoid duplicating live state as timeless fact.
