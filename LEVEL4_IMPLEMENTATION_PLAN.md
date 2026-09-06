# Level 4 Implementation Plan - Vaultline Citadel

This document is the design reference for constructing Level 4. It is a planning document only; scene, script, prefab, input, and ProjectSettings changes must be implemented separately and verified against this plan.

Level 4 extends the progression from Level 2's lateral trap reading and Level 3's timed lane hazards by adding one new player skill: a deliberate jump. The player should vault low obstacles, pass rising map blocks, and time jumps over moving beams while still managing the spiral crowd and gate choices. Vertical movement is the new identity; the track remains straight so the player can understand whether a loss came from steering, timing, or jumping.

## 1. Design goals

- Give Level 4 a distinct identity: a modular vertical obstacle course called **Level 4 - Vaultline Citadel**.
- Teach jumping in isolation before combining it with lateral movement, moving threats, or projectiles.
- Make jumping a real route solution, not a cosmetic animation. A successful jump must let the player clear the authored obstacle and preserve the crowd-loss budget.
- Offer a second solution wherever practical: jump, lane change, or wait for a readable opening. Use a full-width low barrier only after jump input and jump height are verified.
- Preserve the Fermat/golden-angle spiral formation. Do not add per-runner jump physics; the player is the squad leader and hazards use an aggregate, bounded crowd result.
- Use new vertical trap combinations rather than repeating the Level 2 cone/falling/saw route or the Level 3 pulse/spike/hammer route.
- Reuse the existing Gate/Door, crowd, finish, pickup, projectile-pool, HUD, camera, and scene wiring where appropriate.
- Make repeated obstacles prefab-driven, scene-authored, resettable, and bounded for WebGL.
- Keep the level approximately 360m so it is longer than Level 3 without using length as the only difficulty increase.

## 2. Player experience and trap vocabulary

The active Level 4 trap families are:

| Trap family | Player read | Main skill | Initial implementation boundary |
|---|---|---|---|
| Vault barriers | Low modular wall with a bright top edge and ground shadow | Jump timing and commitment | New `JumpBarrierHazard.cs` |
| Rising blocks | Floor block lifts into a lane, pauses, then retracts | Read height and choose jump or lane | New `RisingBlockHazard.cs` |
| Sweep beams | Low beam travels across the track on a visible rail | Jump over a moving lane threat | New `SweepBeamHazard.cs` |
| Shutter blocks | Short half-height panels open and close in a staggered rhythm | Combine jump timing with lane choice | New `ShutterBlockHazard.cs` |
| Side-cannon bolts | Existing charge, aim, and pooled homing projectile | Short lateral pressure while airborne | Existing `ProjectileLauncher.cs` / `TrackingProjectile.cs` |

All new ground obstacles must be low enough to clear with the initial jump tuning. Do not introduce a ceiling hazard that requires crouching, a full-height wall with no opening, or a track gap that depends on per-runner physics in this pass.

### Jump contract

The current `PlayerController` already has `jumpForce`, `gravity`, coyote time, jump buffering, and Space input. Level 4 must turn that prototype jump into an explicit gameplay contract:

- Initial jump force: `8m/s`.
- Initial gravity: `-20m/s²`.
- Expected jump apex above the grounded player: approximately `1.6m`.
- Expected airborne time before returning to the same height: approximately `0.8s`.
- Approximate forward travel while airborne: 4.8m at 6m/s and 8.0m at 10m/s.
- Initial barrier height: `0.85-1.15m`; do not exceed the measured clearance margin.
- One buffered jump and the existing coyote window remain allowed.
- A second jump is not part of Level 4. Do not add double-jump state unless a separate design review approves it.

The jump action must work with keyboard Space and a browser/mobile-friendly Input System action. The current device-null early return in `PlayerController.Update()` must not prevent jump or steering when one device is absent. Preserve mouse drag and keyboard steering, then add the smallest touch/button path needed to make jumping testable on the target WebGL profiles.

### Aggregate crowd interpretation

The lead `CharacterController` owns the physical jump. The crowd remains a bounded visual representation. New hazards should:

1. Query the cached lead jump state and forward crossing time.
2. Determine whether the lead cleared the obstacle's measured vertical envelope.
3. Call the existing narrow crowd-removal API only when the obstacle is failed, with a per-obstacle loss cap.
4. Leave all runners intact for a successful jump unless playtesting identifies a deliberate crowd-edge loss.

Do not add a Rigidbody or individual jump simulation to every visual runner. If a physical collider is used for a barrier, put it on a player-only collision layer or use it only as a lead-player trigger; visual crowd clones must not generate collision or trigger traffic.

## 3. Coordinate convention

Use the same convention as Levels 1-3:

- Track center: x = 0.
- Track surface: y = 0.
- Forward direction: positive z.
- Track width: 10m.
- Recommended lane centers: x = -3, -1, 1, 3.
- Approximate track boundary: x = -5 to x = +5.
- Proposed Level 4 length: 360m.
- Track position: `(0, -0.1, 180)`.
- Player start: approximately `(0, 0, 3)`.
- Finish area: z = 350-358.

All coordinates below are world coordinates when the Level 4 root is at `(0, 0, 0)`. If the root is moved, apply the root offset to every position.

Keep lethal centers at `|x| <= 3.8`. Do not place a kill volume closer than 1.2m to the outer boundary. Keep at least 7m of clear track after every gate and at least 5m before the finish marker.

The track remains straight. Do not introduce curves, physical narrowing, or segmented boundary rules before the crowd boundary system supports them.

## 4. Level flow

| Section | Z range | Purpose |
|---|---:|---|
| Start runway | 0-20 | Establish the citadel palette, jump cue, and spiral width |
| Gate A | 28 | First growth decision before vertical hazards |
| Vault Tutorial | 40-70 | Teach one low barrier, then a required jump |
| Rising Block Yard | 82-116 | Teach blocks that rise and retract |
| Gate B | 128 | Reward the first jump lesson |
| Sweep Beam Alley | 142-180 | Teach a moving low beam and airborne timing |
| Shutter Yard | 192-224 | Combine opening rhythm with lane choice |
| Gate C | 238 | Final growth/economy choice |
| Cross-Pressure Course | 252-296 | Combine barriers, blocks, beams, and one pooled bolt |
| Citadel Lock | 304-344 | Highest difficulty with recovery beats |
| Finish buffer | 350-360 | Clear approach, reward, and finish presentation |

## 5. Shared track and gate coordinates

| Object | Position / size |
|---|---|
| Track | Position `(0, -0.1, 180)`; size `10 x 0.2 x 360` |
| Start line | `(0, 0.01, 3)` |
| Player start | `(0, 0, 3)` |
| Gate A root | `(0, 0, 28)` |
| Gate B root | `(0, 0, 128)` |
| Gate C root | `(0, 0, 238)` |
| Finish marker | `(0, 0.02, 355)` |
| Finish gate | `(0, 0, 358)` |
| Camera look-ahead | Approximately 10-14m; verify at the 10m/s section speed |

### Gate A - first decision

Position: `(0, 0, 28)`.

- Left: `+35`.
- Right: `x2`.
- Gate readable from approximately z = 21.
- Keep z = 35-39 clear so the player can finish the choice before the first barrier.

### Gate B - timing reward

Position: `(0, 0, 128)`.

- Left: `+60`.
- Right: `x2`.
- Gate readable from approximately z = 118.
- Keep z = 135-141 clear before Sweep Beam Alley.

### Gate C - final growth decision

Position: `(0, 0, 238)`.

- Left: `+90`.
- Right: `x2`.
- Gate readable from approximately z = 228.
- Keep z = 245-251 clear before the first combined obstacle.

Use integer multipliers in the first pass because the current `Gate.value` contract is integer-based. Do not silently introduce x2.5 or x3 math as part of Level 4.

## 6. Trap specifications and coordinate sheet

### 6.1 Vault Tutorial

`JumpBarrierHazard` is a low obstacle with a visible top strip, a ground shadow, and an approach marker. It should be obvious that the player can jump over it. The first two barriers offer a lane alternative; the final barrier teaches a required jump after the input contract is verified.

| Z | Barrier placement | Initial dimensions | Design intent |
|---:|---|---|---|
| 46 | x = -2.4 | width 1.8, height 0.85 | Jump or use an adjacent lane |
| 54 | x = 2.4 | width 1.8, height 0.95 | Reverse the lane read |
| 62 | x = 0 | width 3.4, height 1.05 | Jump or move to an outer lane |
| 70 | x = 0 | width 9.0, height 1.10 | First required jump; clear after validation |

Initial settings:

- warning lead: 8m.
- hit check: once when the player crosses the barrier Z plane.
- vertical clearance margin: 0.25m above the barrier top.
- maximum logical runner loss on a failed barrier: 8 runners or 20% of logical count, whichever is lower.
- no loss on a valid jump unless the crowd is already outside the track boundary.

The z = 70 full-width barrier is traversable by jumping, not lethal by contact with the visual mesh. If the current CharacterController cannot clear it at the measured apex, reduce the barrier height before changing the jump force.

### 6.2 Rising Block Yard

`RisingBlockHazard` is a floor-mounted block that lifts into a lane, holds, and retracts. Its state is `Retracted -> Rising -> Raised -> Lowering`, with a simple emissive strip showing the next state. It must never rise instantaneously under the player.

| Z | Block X | Width | Initial cycle / phase | Route |
|---:|---:|---:|---|---|
| 84 | -2.4 | 1.8 | 3.2s / 0.0s | Jump or center/right |
| 94 | 2.4 | 1.8 | 3.2s / 0.8s | Jump or center/left |
| 106 | 0 | 3.6 | 3.6s / 1.6s | Jump or choose an outer lane |
| 116 | -1.2, 1.2 | 1.4 each | 3.8s / 2.0s | Two blocks leave outer lanes |

Initial settings:

- raised height: 1.05m.
- rise duration: 0.45s.
- raised hold: 1.1s.
- lower duration: 0.45s.
- warning lead: 1.0s plus a ground marker at the block's Z plane.
- failed-crossing loss cap: 6 logical runners per block.

The block's visual animation and hit envelope must use the same normalized progress. Do not rely on physics timing to decide whether a block is raised.

### 6.3 Sweep Beam Alley

`SweepBeamHazard` is a low bar on a rail. It travels laterally across a visible endpoint pair and can be cleared by jumping. It is the first moving obstacle that tests the jump apex while the player is already moving forward.

| Object | Root / endpoints | Phase |
|---|---|---:|
| Sweep Beam 1 | root `(0, 0.7, 150)`, x = -3.6 to +3.6 | 0.0s |
| Sweep Beam 2 | root `(0, 0.7, 168)`, x = +3.6 to -3.6 | 1.4s |

Initial settings:

- beam height: 0.65m; beam thickness: 0.25m.
- endpoint-to-endpoint travel: 2.8s.
- endpoint pause: 0.5s.
- full cycle: 6.6s.
- warning lead: 1.0s with endpoint lamps and a direction arrow.
- hit envelope: beam center plus 0.55m runner padding.
- maximum logical runner loss per sweep leg: 5.

Use elapsed-time interpolation and `Mathf.Lerp`, not accumulated velocity. Pause must freeze the timer and beam position; restart must restore the authored endpoint and phase. The beam must never span the complete width, and the two beams must not form a simultaneous wall.

### 6.4 Shutter Yard

`ShutterBlockHazard` is a half-height panel that alternates between raised and lowered states. The panel is a short map block, not a door authority; it should be easy to understand from its colored open/closed strip. The player can jump a closed panel or shift to an open lane.

| Z | Panels | Initial phase | Design intent |
|---:|---|---:|---|
| 194 | x = -2.4 | 0.0s | First closed-lane read |
| 204 | x = 2.4 | 0.75s | Alternating lane |
| 214 | x = 0 | 1.5s | Jump or outer-lane recovery |
| 224 | x = -2.4, 2.4 | 2.25s | Two-panel rhythm; center remains open |

Initial settings:

- panel width: 1.8m.
- panel height: 1.0m.
- open duration: 1.1s.
- close duration: 0.35s.
- raised hold: 1.25s.
- warning lead: 0.8s.
- maximum logical runner loss per closed-panel crossing: 6.

Do not place the panels close enough in Z that the player must jump continuously without a landing/recovery beat. The first pass must leave at least 6m between crossing planes.

### 6.5 Cross-Pressure Course

This is the first multi-family combination. The player should receive one dominant action at each obstacle rather than four simultaneous warnings.

| Z | Hazard | Placement / phase |
|---:|---|---|
| 254 | Vault barrier | x = -2.4, width 2.0; jump or center/right |
| 262 | Rising block | x = 2.4; phase 0.6s |
| 272 | Sweep beam | root z = 278; starts left; approach line z = 264 |
| 282 | Single side cannon | left wall at x = -4.35; phase 1.5s; one pooled bolt |
| 290 | Shutter pair | x = -1.2 and +1.2; center/outer recovery route |
| 296 | Recovery marker | clear; optional pickup only |

The beam warning must remain visible above the barrier telegraph. The cannon cannot fire in the same frame as a beam hit check. The shutter pair must leave a route that does not require a second jump immediately after the sweep beam.

### 6.6 Citadel Lock

The final section is the climax and uses all five families, but includes clear recovery beats and a permanently clear finish approach.

| Z | Hazard | Placement / phase |
|---:|---|---|
| 304 | Vault barrier row | x = -2.4 and +2.4; center lane open; staggered heights 0.95/1.05m |
| 312 | Sweep beam | root z = 318; endpoints -3.6/+3.6; starts right |
| 320 | Rising block | x = 0; phase 1.0s; outer lanes open |
| 328 | Cannon pair | x = -4.35 and +4.35; only one bolt active in the immediate window |
| 336 | Shutter pair | x = -2.4 and +2.4; center route open at start |
| 344 | Recovery runway | completely clear |
| 350 | Finish marker | clear |
| 358 | Finish gate | clear |

The final beam's approach line is z = 304. Its endpoint lamps must be visible before the barrier row becomes active. The final cannon pair must not target the same runner in the same frame. The last 14m before the finish marker must not contain a lethal object, projectile launch, or crowd-removal check.

## 7. Difficulty progression and crowd-loss budget

Increase difficulty in this order:

1. Teach one jump signal with a broad route.
2. Require a jump only after jump input and apex are verified.
3. Alternate barrier and block lanes.
4. Introduce a moving beam with visible endpoints.
5. Combine one jump threat with one lateral threat and a recovery beat.
6. Add a single pooled bolt to test movement while airborne.
7. Reduce recovery space only in the final 20%.

Do not shorten warnings, increase obstacle height, increase forward speed, and reduce safe lanes in the same tuning pass.

| Section | Safe routes | Hazard count | Jump demand | Difficulty |
|---|---:|---:|---|---|
| Vault Tutorial | 2-3, then jump-required | 4 barriers | One jump | Medium |
| Rising Block Yard | 2-3 | 4 blocks | Optional jump / timing | Medium-high |
| Sweep Beam Alley | 2-3 | 2 beams | Moving jump | High |
| Shutter Yard | 2-3 | 5 panels | Jump or lane timing | High |
| Cross-Pressure Course | 2 | 6 hazards | Jump + lane + one bolt | Very high |
| Citadel Lock | 1-2 at each event, never zero | 8-10 hazards | All families with recovery | Very high |

Initial logical crowd-loss targets:

- Tutorial sections: 5-15% for a competent route.
- Sweep Beam Alley and Shutter Yard: 10-25%.
- Final combination: 15-35%.
- A single obstacle activation must not remove more than 35% of the logical crowd.
- A successful jump should remove zero runners from that obstacle; visual formation compression is acceptable, but it must not be mistaken for logical loss.

## 8. Reusable prefab strategy

Create reusable prefab assets rather than duplicating unique trap hierarchies in the scene. The preferred folder is `Assets/Prefabs/Traps/Level4/`.

### Shared obstacle prefab contract

Each reusable obstacle root should contain:

- `Hazard` root script with serialized dimensions, timing, warning lead, loss cap, and reset state.
- `Visual` child containing the shared mesh/material references.
- `Telegraph` child containing endpoint lamps, ground marker, or emissive warning strip.
- `HitVolume` child containing a trigger or logical crossing configuration; it must not process visual crowd clones individually.
- Optional `PlayerCollision` child on a player-only layer for low physical barriers.
- No runtime `Instantiate` or `Destroy` path.

### Prefab assets and variants

| Prefab | Reusable variants | Shared data |
|---|---|---|
| `JumpObstacleBase.prefab` | `VaultBarrier.prefab`, `VaultBarrier_Wide.prefab` | warning, height, width, loss cap |
| `RisingBlockBase.prefab` | `RisingBlock_Single.prefab`, `RisingBlock_Double.prefab` | normalized motion, phases, raised height |
| `SweepBeamBase.prefab` | `SweepBeam_LeftStart.prefab`, `SweepBeam_RightStart.prefab` | endpoints, travel, pause, hit padding |
| `ShutterBlockBase.prefab` | `ShutterBlock_Single.prefab`, `ShutterBlock_Pair.prefab` | open/close cycle, phase, panel width |
| `ObstacleTelegraph.prefab` | barrier, beam, and shutter child variants | shared low-cost warning material/effect |

Use prefab variants for dimensions, phase, endpoints, and lane placement. Keep runtime state on the scene instance/component, not in a shared ScriptableObject. If the same values are balanced across Level 4 and a later level, promote only those values to a focused ScriptableObject after two scenes demonstrate the need.

Reuse these existing assets and authorities:

- Existing Gate/Door prefab and gate math; do not create a Level4Gate.
- Existing `ProjectileLauncher` and `TrackingProjectile` pool for the limited cannon section; do not create a second projectile pool.
- Existing finish, pickup, CounterBadge, Player, camera, EventSystem, wallet, and HUD wiring from the verified Level 3 copy.
- Existing simple URP materials where they match the citadel palette; use one shared obstacle material plus a small telegraph material set.

Do not refactor every Level 2/3 hazard into prefabs as unrelated cleanup. Convert only repeated Level 4 obstacle families and any existing asset that must be reused by this scene. A prefab instance should expose only the tuning fields needed by level design; mesh internals and warning wiring stay inside the prefab.

## 9. Scene construction plan

When implementation is approved:

1. Duplicate the verified `Assets/Scenes/Level3.unity` to `Assets/Scenes/Level4.unity`; verify the copy opens before removing Level 3 content.
2. Preserve `Level3.unity` as a workbench. Do not modify its trap instances while constructing Level 4.
3. Rename the root to `Level 4 - Vaultline Citadel`.
4. Replace the track with the 10m x 360m track and verify start, finish, camera, and progress references.
5. Remove Level 3 production trap instances from the Level 4 copy. Reuse only the explicitly planned pooled cannon and shared authorities.
6. Create these parents:

    `Systems`

    `Track`

    `Gates`

    `Trap Sections/Vault Tutorial`

    `Trap Sections/Rising Block Yard`

    `Trap Sections/Sweep Beam Alley`

    `Trap Sections/Shutter Yard`

    `Trap Sections/Cross-Pressure Course`

    `Trap Sections/Citadel Lock`

    `Pickups`

    `Lighting`

    `FinishGate`

7. Implement and test jump input/state exposure before building the full-width z = 70 barrier.
8. Create the four reusable Level 4 prefab families and validate one instance of each in an isolated test section.
9. Build the tutorials from the coordinate tables. Do not add extra hazards during the first graybox pass.
10. Add the three gates at z = 28, 128, and 238 using existing Gate/Door behaviour.
11. Build Cross-Pressure Course, then Citadel Lock, keeping recovery markers visible and clear.
12. Add the one planned cannon section only after jump obstacle hit checks pass; verify pool reuse and bolt counts.
13. Add pickups after obstacle readability and crowd-loss budgets pass.
14. Update Level 4 HUD, world label, camera/Cinemachine names, and finish copy without adding another UI controller.
15. Add `Level4.unity` to Build Settings only after the scene is playable and the approved scene order is known. Creating the scene does not make it part of the build.

## 10. Script and architecture boundary

The project remains a small-game prototype. Keep the implementation thin and explicit:

- `PlayerController.cs`: owns lead movement, jump buffering, coyote time, vertical velocity, and a read-only jump-state query for hazards.
- `PlayerCrowdManager.cs`: remains the owner of logical count, visual clone reuse, spiral placement, runner loss, and game-over checks. It does not simulate individual jumps.
- `JumpBarrierHazard.cs`, `RisingBlockHazard.cs`, `SweepBeamHazard.cs`, and `ShutterBlockHazard.cs`: own only local state, warning presentation, crossing evaluation, and bounded crowd-removal calls.
- `ProjectileLauncher.cs` / `TrackingProjectile.cs`: remain the sole projectile authority.
- `Gate.cs` / `Door.cs`: remain the sole gate authority.
- `LevelHud.cs`: owns Level 4 HUD copy and current HUD presentation; `UIManager` remains compatibility-only.

Communication rules:

- Hazards may call the existing narrow crowd-removal API, but must not directly update HUD text, currency, pause state, or finish state.
- Hazards cache `PlayerController` and `PlayerCrowdManager` references and return early when dependencies or game state are unavailable.
- Use direct serialized references for authored scene dependencies. Do not add repeated `Find*` calls inside per-frame loops.
- Use a single normalized timer per moving obstacle. Pause freezes timers and positions; scene restart resets all states to authored values.
- Prefer one crossing check per obstacle event or movement leg over continuous per-runner physics.

Required safeguards:

- Add `OnValidate` clamps for obstacle height, width, travel duration, warning lead, phase, loss cap, endpoints, and any pool size.
- A hazard must guard against repeated loss on the same crossing, sweep leg, or shutter cycle.
- A failed jump must not repeatedly remove runners while the player remains inside an obstacle volume.
- Physical collision must be restricted to the lead player or replaced with a logical crossing check.
- New warning visuals must be pooled or scene-authored and must use shared materials.

## 11. Speed and movement assumptions

Use the Level 3-style distance ramp initially so vertical obstacle novelty is isolated from another speed model:

| Player Z | Target forward speed |
|---:|---:|
| 3 | 6.0m/s |
| 93 | 7.0m/s |
| 183 | 8.0m/s |
| 273 | 9.0m/s |
| 350 | 9.9m/s |
| 358 | 10.0m/s |

Initial formula:

```text
progress = clamp01((playerZ - 3) / (358 - 3))
normalSpeed = lerp(6.0, 10.0, progress)
```

Keep horizontal steering at the existing value until playtesting shows that the final jump/steering combinations are unreadable. Do not increase horizontal speed and jump force in the same tuning pass. Barrier warning lead must be verified against the fastest 10m/s case.

## 12. WebGL and runtime constraints

- Keep Level 4 obstacles scene-authored; only the existing bounded cannon projectiles may be pooled runtime objects.
- Do not add per-runner colliders, Rigidbody components, or jump animations to the visual crowd.
- Avoid per-frame `Instantiate`, `Destroy`, LINQ, string formatting, and repeated scene searches in new scripts.
- Use one bounded crowd scan per obstacle crossing or sweep leg, not continuous physics for every runner.
- Keep at most two moving obstacles performing hit checks in the same frame; the final section may show more warnings but must stagger evaluations.
- Keep the Level 4 projectile pool at or below 24 global projectiles and no more than one planned active bolt in an immediate obstacle window.
- Use shared URP-compatible materials, simple emissive warnings, and low-cost ground markers. Avoid full-screen post-processing and large particle bursts.
- Do not enable WebGL threads, decompression fallback, or a larger memory heap to solve Level 4 issues.
- Profile small, normal, and compressed-large crowds for active visual runners, obstacle checks, projectile count, GC allocations, and frame time.

## 13. Playtesting gates

Before accepting Level 4:

- The scene opens and restarts cleanly with no missing script, prefab, input, or material references.
- The HUD and world label show `LEVEL 4 - VAULTLINE CITADEL`; no Level 3 copy appears during play, pause, restart, or finish.
- Space and the browser/mobile jump action both trigger the same jump state; an absent mouse or keyboard device does not disable the other supported input.
- Measured jump apex is at least 0.25m above the tallest first-pass barrier, with the initial force/gravity values recorded.
- Coyote time and jump buffering work without allowing a second jump.
- The z = 70 full-width low barrier is clearable at 6m/s and 10m/s, and its failure route is understandable after one attempt.
- Rising blocks use the same normalized state for animation and hit checks; no block pops under the player without warning.
- Sweep beams reach both endpoints deterministically, pause correctly, and do not create an unavoidable wall with another beam.
- Shutter blocks leave a reliable jump or lane route at every crossing.
- A successful jump does not remove logical runners; a failed crossing respects the per-obstacle loss cap.
- No visual runner creates obstacle collision or trigger spam.
- Both left and right choices at all three gates can complete the level.
- The final 14m remains clear for finish detection, crowd result, and result UI.
- No new recurring gameplay GC spikes, console errors, or physics-trigger floods appear.
- Test at 960 x 600 and a narrow/mobile-like aspect ratio.
- Test a Chromium WebGL build and a lower-powered browser profile when the build is available.

### Route test matrix

| Test route | Input intent | Expected result |
|---|---|---|
| Left-first | Take left at all gates; favor left lanes and jump barriers | Completes with moderate crowd loss |
| Right-first | Take right at all gates; favor right lanes and jump barriers | Completes with moderate crowd loss |
| Jump-focused | Hold a central route and jump every required barrier | Completes if timing is correct; validates vertical route |
| Mixed | Alternate gate sides and combine steering with selective jumps | Completes; validates route independence |
| Hold-center negative | Avoid steering and jumping | Expected losses; not evidence that the level is unbeatable |
| Large-crowd route | Enter Citadel Lock with the highest safe test count | No unbounded clones, instant full wipe, or frame spike |
| Pause/restart | Pause during rising, sweeping, shutter, and airborne states, then restart | Timers freeze; restart restores authored states |

## 14. Implementation order and verification

Implement the plan in this order:

1. Duplicate Level 3 to `Level4.unity` and verify the copy opens before changing content.
2. Add the Level 4 input action and expose a read-only lead jump state from `PlayerController`.
3. Measure jump apex, airborne time, and forward travel at 6m/s and 10m/s with a focused test or editor probe.
4. Create `JumpObstacleBase`, `RisingBlockBase`, `SweepBeamBase`, `ShutterBlockBase`, and their first variants.
5. Build Vault Tutorial with one barrier, then the required full-width barrier only after the jump test passes.
6. Build Rising Block Yard and verify normalized state/hit alignment.
7. Build Sweep Beam Alley with one beam, verify endpoint travel/pause/restart, then add the second beam.
8. Build Shutter Yard and verify each panel leaves a jump or lane route.
9. Add gates B and C and confirm both sides remain completable before combination work.
10. Build Cross-Pressure Course from the coordinate table without adding unplanned hazards.
11. Build Citadel Lock with a clear finish approach and one dominant warning at a time.
12. Add the limited pooled cannon pressure and verify it cannot overlap a jump hit check unfairly.
13. Add optional pickups only after readability and crowd-loss budgets pass.
14. Run route, pause/restart, HUD, profiler, and WebGL checks.
15. Review Build Settings order only after the scene is accepted; do not assume `Level4.unity` is included.

## 15. Review decisions before implementation

Proposed defaults for approval:

- 360m straight track.
- Scene and player-facing name: `Level 4 - Vaultline Citadel`.
- Gates at z = 28, 128, and 238 with `+35/x2`, `+60/x2`, and `+90/x2`.
- New primary trap families: vault barriers, rising blocks, sweep beams, and shutter blocks.
- One limited reuse of the existing pooled side-cannon path in the combined section.
- Initial jump force 8, gravity -20, no double jump.
- Level 4 uses the 6-10m/s distance ramp as a scene-specific configuration.
- Reusable prefabs live under `Assets/Prefabs/Traps/Level4/` and use variants for lane, size, phase, and endpoints.

Confirm separately:

- Whether the mobile/browser jump action should be a visible HUD button or an input-system binding supplied by the host page.
- Whether the full-width z = 70 barrier feels fair at the measured jump apex and 10m/s speed.
- Whether a successful jump should preserve all logical runners or allow a small crowd-edge loss for very wide formations.
- Whether the limited cannon section improves pressure or should be removed if the 960 x 600 warning load is too high.
- Whether Level 4 should follow Level 3 in Build Settings or remain a workbench scene.

## 16. Acceptance record template

Do not mark this plan implemented until measured results replace the following `TBD` values:

| Check | Result | Evidence / file |
|---|---|---|
| Jump apex | TBD | Editor probe or timing test |
| Airborne duration | TBD | Editor probe or timing test |
| Forward travel at 6m/s / 10m/s | TBD | Movement test |
| Barrier clearance at low/high speed | TBD | Route test |
| Rising block state/hit alignment | TBD | Playtest log |
| Sweep outbound/return travel | TBD | Timing test |
| Shutter cycle duration | TBD | Timing test |
| Maximum logical loss per obstacle | TBD | Playtest log |
| Maximum active projectiles | TBD | Profiler/editor observation |
| Small/normal/large crowd frame time and GC | TBD | Profiler capture |
| 960 x 600 HUD and jump-control check | TBD | Screenshot/browser note |
| Narrow/mobile-like HUD and jump-control check | TBD | Screenshot/browser note |
| Chromium WebGL pass | TBD | Build/browser test note |
| Lower-powered browser pass | TBD | Build/browser test note |
| Build Settings scene order | TBD | `ProjectSettings/EditorBuildSettings.asset` |
