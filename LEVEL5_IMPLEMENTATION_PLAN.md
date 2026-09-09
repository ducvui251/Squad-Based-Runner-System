# Level 5 Implementation Plan — Fracture Relay

Implementation clarification: visual runners remain collider-free and do not receive separate jump physics. They stay parented to the player and inherit the shared lead jump, while a bounded per-runner crossing session classifies each runner from its actual world-space Z and shared trajectory. A successful lead crossing does not guarantee the rear crowd survives; only runners that cannot clear the far lip are transferred to the existing pooled fall path.

This document is the proposed design reference for constructing Level 5. It is a planning document only; scene, script, prefab, input, and ProjectSettings changes must be implemented separately and verified against this plan.

Level 5 recombines the hazard vocabulary established by Levels 2–4 instead of introducing a large new set of traps. Its new skill is route switching across three dimensions: lane choice, timed movement, and deliberate jumping. The signature set piece is a full-width physical road gap that the lead player must jump to continue. The lead uses normal CharacterController gravity while visual runners that remain over the gap fall through the crowd manager's existing pooled falling-runner path.

## 1. Design goals

- Give Level 5 a distinct identity: an unstable elevated causeway called Level 5 - Fracture Relay.
- Use every established hazard family at least once: cones, hidden falling traps, circular saws, pulse plates, spike shuttles, swinging hammers, side-cannon bolts, vault barriers, rising blocks, sweep beams, and shutter blocks.
- Combine old hazards in new pairings and chains, especially rising block plus circular saw and timed lane pressure immediately before or after a required jump.
- Teach the fracture mechanic in isolation before placing it in the final chain.
- Make the player jump to continue across a full-width fracture; do not offer a lane bypass around the fracture.
- Preserve a readable escape route for every ordinary hazard and a valid jump route for every required-jump event.
- Keep the Fermat/golden-angle spiral formation and logical-to-visual crowd compression unchanged. Hazards evaluate the lead player and apply bounded logical loss; visual crowd runners do not receive individual jump physics.
- Increase difficulty through combinations, phase offsets, and recovery-space choices rather than full-width lethal walls or uncontrolled object counts.
- Reuse the existing gate, door, crowd, finish, pickup, camera, HUD, input, and projectile authorities.
- Stay within the WebGL constraints: scene-authored hazards, bounded pooled projectiles, shared materials, no gameplay-time Instantiate/Destroy for recurring objects, and no native-only dependency.

## 2. Progression and combination strategy

The level deliberately changes the order in which earlier traps are read.

| Combination | Earlier families | Player action | Design purpose |
|---|---|---|---|
| Cone + pulse plate | ConeHazard, PulsePlateHazard | Shift lane while reading a charge/discharge flash | Replaces the earlier cone-only weave with a timing choice |
| Hidden falling trap + saw | FallingHazard, CircularSaw | React to a lane-independent reveal, then dodge a moving threat | Makes the Level 2 surprise trap affect a planned saw route |
| Rising block + saw | RisingBlockHazard, CircularSaw | Jump or shift around the block, then track the saw endpoint | Required signature combination; never creates a simultaneous full-width wall |
| Hammer + shutter | SwingHammerHazard, ShutterBlockHazard | Time a jump or take the currently open lane | Couples overhead timing with a ground lane rhythm |
| Spike shuttle + cannon | SpikeSweepHazard, ProjectileLauncher / TrackingProjectile | Read the shuttle direction, then make a short lateral dodge | Uses two moving threats with staggered hit checks |
| Barrier + sweep beam | JumpBarrierHazard, SweepBeamHazard | Jump a static obstacle, recover, then jump a moving beam | Builds the lead-in to the fracture |
| Fracture + any follow-up lane threat | New CrackedSpanHazard plus a prior family | Jump the only crossing, land, then steer | Tests whether the player can switch from vertical to lateral control |

The final section is a relay of these combinations, not a pile of simultaneous hit volumes. At each crossing there is one dominant next action. Warnings for the following event may be visible, but only bounded and staggered hit checks may run in the same frame.

## 3. Coordinate convention

Use the convention from Levels 1–4:

- Track center: x = 0.
- Track surface: y = 0.
- Forward direction: positive z.
- Track width: 10m.
- Recommended lane centers: x = -3, -1, 1, 3.
- Approximate track boundary: x = -5 to x = +5.
- Proposed Level 5 length: 420m.
- Track position: (0, -0.1, 210).
- Player start: approximately (0, 0, 3).
- Finish area: z = 408–420.
- Finish marker: z = 416.
- Finish gate: z = 418.

All coordinates below are world coordinates when the Level 5 root is at (0, 0, 0). If the root moves, apply the root offset to every position.

Keep lethal centers at |x| <= 3.8. Do not place a kill volume closer than 1.2m to the outer boundary. Keep at least 7m of clear track after each gate and at least 16m before the finish marker. The track remains straight; its road surface is divided only at the two authored 4.0m fracture gaps.

## 4. Level flow

| Section | Z range | Purpose |
|---|---:|---|
| Start runway | 0–20 | Establish the unstable-causeway theme, crowd width, and jump cue |
| Gate A | 30 | First growth decision |
| Crosswind Pulse Weave | 42–80 | Combine lane weaving with pulse timing |
| Drop-Saw Relay | 90–132 | Recombine hidden falling traps, cones, and saws |
| Gate B | 150 | Reward the first two combination lessons |
| Block-Saw Interlock | 162–204 | Teach the required rising-block plus saw pairing |
| Pendulum Shutter Exchange | 216–266 | Combine hammers, shutters, a spike shuttle, and one cannon |
| Fracture Tutorial | 276–310 | Teach the only-crossing jump over a visual track fracture |
| Gate C | 320 | Final growth/economy decision |
| Final Fracture Relay | 334–402 | Chain every established family with two fracture moments |
| Finish buffer | 402–420 | Clear result and finish presentation |

The proposed length is longer than Level 4 because the level needs teaching, recovery, and a final relay. Do not shorten recovery spaces to compensate for a scene that has too few playable beats. If the speed ramp makes the final chain unreadable, move a section boundary before increasing hazard speed or removing a safe route.

## 5. Shared track and gate coordinates

| Object | Position / size |
|---|---|
| Track | Position (0, -0.1, 210); size 10 x 0.2 x 420 |
| Start line | (0, 0.01, 3) |
| Player start | (0, 0, 3) |
| Gate A root | (0, 0, 30) |
| Gate B root | (0, 0, 150) |
| Gate C root | (0, 0, 320) |
| Finish marker | (0, 0.02, 416) |
| Finish gate | (0, 0, 418) |
| Camera look-ahead | Approximately 12–16m; verify at 10m/s |

### Gate A - first combination decision

Position: (0, 0, 30).

- Left: +45.
- Right: x2.
- Readable from approximately z = 22.
- Keep z = 37–41 clear before the first pulse/cone sequence.

### Gate B - interlock decision

Position: (0, 0, 150).

- Left: +75.
- Right: x2.
- Readable from approximately z = 140.
- Keep z = 157–161 clear before the first rising block.

### Gate C - fracture relay decision

Position: (0, 0, 320).

- Left: +110.
- Right: x2.
- Readable from approximately z = 309.
- Keep z = 327–333 clear before the final relay.

Use integer multipliers in the first pass because the current Gate.value contract is integer-based. Do not add x2.5 or x3 math as part of this level plan. Both sides of all three gates must retain a survival route.

## 6. Trap specifications and coordinate sheet

### 6.1 Crosswind Pulse Weave

This opening section teaches the new cone-plus-pulse combination with broad routes. The first pulse is isolated; later pulses are paired with alternating cone rows.

| Z | Hazard | Placement / phase |
|---:|---|---|
| 44 | Pulse plate | x = 0; phase 0.0s |
| 50 | Cone row | x = -3.2 and +3.2 |
| 58 | Pulse plate | x = -2.4; phase 0.55s |
| 64 | Cone row | x = -1.2 and +3.2 |
| 72 | Pulse plate | x = +2.4; phase 1.10s |
| 78 | Cone row | x = -3.2 and +1.2 |

Pulse settings inherit the Level 3 first-pass contract:

- Plate radius: approximately 0.95m.
- Kill radius: approximately 1.05m.
- Charge warning: 0.75s.
- Discharge duration: 0.35s.
- Recovery duration: 1.35s.
- Full cycle: 2.45s.
- Maximum logical loss per discharge: 2 runners.
- Check a plate once per discharge, not continuously for every frame.
- Use shared emissive materials and small ground rings.
- Keep at least two technically safe lanes at every plate/cone crossing.

Cone settings inherit the Level 2 contract: killRadius approximately 0.85m and no cone placement closer than 1.2m to the crowd boundary. The section should teach the player to move before the pulse flashes, then correct laterally around the cone row.

### 6.2 Drop-Saw Relay

This section combines Level 2's lane-independent hidden reveal with a moving saw. Falling traps remain hidden until their forward trigger and spend themselves after one drop.

| Z | Hazard | Placement |
|---:|---|---|
| 94 | Hidden falling trap 1 | x = -2.4; trigger line z = 87 |
| 100 | Cone row | x = -3.2 and +3.2 |
| 110 | Circular saw 1 | centered root; explicit x limits -3.6 to +3.6 |
| 122 | Hidden falling trap 2 | x = +2.4; trigger line z = 115 |
| 128 | Cone row | x = -1.2 and +1.2; outer lanes remain open |

Falling trap settings:

- Renderers and shadow-producing child renderers disabled before activation.
- Activation is lane-independent and uses forward progress.
- floatingHeight = 5m, gravity = -20m/s², killRadius approximately 1.2m.
- Reveal occurs before the drop and the trap activates once per run.
- Keep at least 16m between hidden falling traps unless measured trigger timing proves a shorter separation safe.
- Use the existing one-shot reset-on-restart behaviour from Level 2.

Saw settings:

- useExplicitXLimits = true.
- leftLimitOverride = -3.6.
- rightLimitOverride = +3.6.
- moveSpeed initially 3–4m/s.
- killRadius approximately 0.8m.
- Use one moving saw in the immediate falling-trap window; the saw must not coincide with a second moving wall.

The first falling trap creates the lane change, the cone row confirms that choice, and the saw provides the moving follow-up. The second falling trap should be visible only when its own trigger is crossed; it must not be revealed by the first trap.

### 6.3 Block-Saw Interlock

This is the first signature pairing requested for Level 5. A rising block occupies one lane while a saw approaches the same decision. A player can jump the block or shift to a lane that the saw does not currently occupy.

| Z | Hazard | Placement / phase |
|---:|---|---|
| 164 | Rising block A | x = -2.4, width 1.8m, phase 0.0s |
| 176 | Circular saw 2 | root z = 176; starts right, endpoints -3.6 / +3.6 |
| 188 | Pulse row | x = -1.2 and +1.2; phases 0.0s / 0.8s |
| 198 | Rising block B | x = +2.4, width 1.8m, phase 1.6s |

Rising block settings inherit Level 4:

- raised height: approximately 1.05m.
- rise duration: 0.45s.
- raised hold: 1.1s.
- lower duration: 0.45s.
- warning lead: 1.0s plus a ground marker.
- failed-crossing loss cap: 6 logical runners per block.
- Animation and hit envelope use the same normalized progress.

The saw warning must show its current endpoint and travel direction without covering the rising-block marker. A player who jumps the block must still have enough forward distance to land before the pulse row. A player who changes lane around the block must be able to read the saw's current lane. Do not tune the block, saw speed, and pulse phase simultaneously during the first balance pass.

### 6.4 Pendulum Shutter Exchange

This section creates a vertical-and-lateral relay. The player alternates between overhead timing, open-lane reading, and the moving shuttle. The cannon adds one directed dodge after the shuttle instead of firing into the whole section.

| Z | Hazard | Placement / phase |
|---:|---|---|
| 220 | Swing hammer | pivot x = -2.4; phase 0.0s |
| 230 | Shutter panel | x = +2.4; phase 0.75s |
| 244 | Spike shuttle | root z = 244; endpoints -3.8 / +3.8; starts left |
| 254 | Side cannon | x = +4.35, y = 1.4; phase 1.0s |
| 264 | Shutter pair | x = -1.2 and +1.2; phase 1.8s |

Hammer settings inherit Level 3:

- pivot height: 3.2m.
- arm length: 2.0m.
- swing angle: -55 to +55 degrees.
- full oscillation: 2.6s.
- warning lead: 0.8s before the 16m approach zone.
- maximum loss per swing cycle: 3 logical runners.
- Keep two lanes open at each crossing.

Shutter settings inherit Level 4:

- panel width: approximately 1.8m.
- panel height: approximately 1.0m.
- open duration: 1.1s.
- close duration: 0.35s.
- raised hold: 1.25s.
- warning lead: 0.8s.
- maximum loss per closed-panel crossing: 6 logical runners.
- Keep at least 6m between crossing planes and do not require continuous airborne time.

Spike shuttle settings remain an invariant from Level 3:

- Endpoint-to-endpoint travel: exactly 3.00s in both directions.
- Endpoint dwell: 0.75s.
- Full cycle: 7.50s.
- Carriage width: approximately 1.7m.
- Maximum loss per directional leg: 4 logical runners.
- Use elapsed-time interpolation, not accumulated velocity or Rigidbody motion.
- Pause freezes the timer and position; restart restores the authored endpoint and phase.

Use the existing pooled projectile path for the cannon. Level 5 should have no more than one active bolt in the immediate player window in this section.

### 6.5 Fracture Tutorial

The first fracture is deliberately isolated from the final chain. The player sees the jump contract before the track appears to break.

| Z | Hazard | Placement |
|---:|---|---|
| 276 | Vault barrier | x = -2.4; width 1.8m; height 0.95m |
| 288 | Sweep beam | root z = 288; endpoints +3.6 / -3.6; starts right |
| 304–308 | Cracked span 1 | full-width visual fracture; 4.0m forward span |
| 310–320 | Recovery | completely clear before Gate C |

Vault barrier settings inherit Level 4:

- warning lead: 8m.
- hit check once at the barrier crossing.
- vertical clearance margin: 0.25m above the barrier top.
- maximum loss: 8 logical runners or 20% of logical count, whichever is lower.
- A valid jump removes no runners from the barrier.

Sweep beam settings inherit Level 4:

- beam height: 0.65m.
- beam thickness: 0.25m.
- endpoint travel: 2.8s.
- endpoint pause: 0.5s.
- full cycle: 6.6s.
- warning lead: 1.0s with endpoint lamps and a direction arrow.
- maximum loss per sweep leg: 5 logical runners.

The barrier is optional lane movement or jump. The beam is a moving jump or lane event. The fracture at z = 304 is the first event with no lateral bypass.

### 6.6 Cracked span contract

CrackedSpanHazard is the only new hazard behaviour planned for Level 5. It must make the lead player jump to continue without simulating a physical jump for every visual runner. The track itself is physically split at the span, so the lead and the visual crowd can have different outcomes.

Player-facing contract:

- Show a dark recessed crack or broken-span visual from crackStartZ to crackStartZ + 4.0m across x = -4.6 to +4.6.
- Place a bright edge strip and two low-cost approach markers beginning 10m before the near edge.
- Require the lead player to be airborne and above the measured clearance height when crossing the center plane of the span.
- The initial jump contract is inherited from Level 4: jumpForce = 8m/s, gravity = -20m/s², expected apex approximately 1.6m, and same-height airborne time approximately 0.8s.
- Use requiredClearance = 1.25m as the initial value, then verify it against the measured PlayerController state and the visual crack lip.
- A jump from approximately 1m before the near edge should clear the 4.0m span at both 6m/s and 10m/s. Do not increase the span length until this is measured.
- A successful lead crossing does not guarantee a full-crowd crossing. Full hold should preserve the crowd where the authored formation and jump geometry permit it; early release may leave the lead and front runners safe while trailing runners fail.
- A grounded lead that reaches the missing road enters the normal CharacterController fall path; the fracture does not add a second arbitrary loss on top of runners falling through the gap.
- Visual runners are classified as SAFE_NEAR_SIDE, OVER_GAP, SAFE_FAR_SIDE, or FAILED from actual world-space Z, the shared PlayerController trajectory, road height, and far-edge clearance. Only FAILED runners are removed from the active formation and sent through PlayerCrowdManager's pooled gravity-fall path. Evaluation continues after the lead reaches the far side so an early release can split the crowd across the road.
- Releasing Space or the touch press while ascending cuts the upward velocity to the authored release multiplier (initially 0.35); holding the input preserves the full jump arc.

First-pass technical implementation:

1. Replace the legacy 420m road collider with three solid road segments: 0–304, 308–386, and 390–420. Do not leave a collider or renderer spanning either 4.0m gap.
2. Keep the crack visual at each gap, but recess its bed and core below the road surface so it reads as a pit rather than a painted line.
3. Keep the legacy LeadStopper and Bridge children only as disabled compatibility objects. They must not provide the Level 5 crossing surface or block the player.
4. Use one cached PlayerController and PlayerCrowdManager reference. Begin a bounded per-fracture crossing session before the near edge, evaluate the lead at the center/far crossing, and pass the authored near/far Z bounds plus road-clearance values to the crowd API.
5. Visual crowd runners must remain collider-free. The manager keys each participating runner by identity, latches SAFE_FAR_SIDE/FAILED exactly once, and preserves the rounded logical-per-visual loss rule for FAILED transfers.
6. Reset the fracture state and restore the authored segmented-road scene on restart. Do not rebuild or instantiate road geometry during gameplay.
7. If a later build needs a recovery plane, it must be below the visible pit and must not cover or bridge the authored road gap.

This physical-gap and partial-crowd revision is approved for the current Level 5 implementation. It keeps the road straight and the gap dimensions deterministic, uses no per-runner Rigidbody simulation, and relies on the existing lead fall/game-over guard plus pooled visual-runner fall motion. The gap, early-release jump cut, spatial split, and restart behaviour must be measured in editor playtests before WebGL acceptance.

### 6.7 Final Fracture Relay

The final section uses all established trap families through staggered pairings. The final two events are a second fracture and a spike shuttle, followed by a long clear finish approach.

| Z | Hazard | Placement / phase |
|---:|---|---|
| 336 | Cone row | x = -3.2 and +3.2 |
| 344 | Pulse row | x = -2.4 and +2.4; phases 0.0s / 0.8s |
| 352 | Hidden falling trap | x = +2.4; trigger line z = 345 |
| 360 | Side cannon | left wall x = -4.35, y = 1.4; one pooled bolt |
| 368 | Rising block | x = 0; width 3.4m; phase 1.0s; outer lanes open |
| 378 | Circular saw | root z = 378; starts left; endpoints -3.6 / +3.6 |
| 386–390 | Cracked span 2 | full-width visual fracture; mandatory jump |
| 400 | Spike shuttle | root z = 400; endpoints +3.8 / -3.8; starts right |
| 402–416 | Finish buffer | no new hazard activation or crowd-removal check |
| 416 | Finish marker | clear |
| 418 | Finish gate | clear |

The final chain is intentionally different from earlier levels:

- Cones and pulse plates make the first lane choice time-sensitive.
- The hidden falling trap reveals independently of lane, followed by one cannon bolt.
- The rising block and saw repeat the signature combination at the highest crowd-width pressure, but at different Z planes so the player can identify each cause.
- The second fracture requires a jump with no side route.
- The spike shuttle is visible during the approach but its hit checks begin only in its immediate crossing window. It must not create an unavoidable wall with the preceding fracture.
- Stop all final-section hazard hit checks once the lead player has passed the final event boundary at z = 402. The last 14m before the finish marker must remain clear.

For the final saw/block pairing, retain at least one lane that is clear of the block and the saw's current position. If the visual crowd is too wide for that lane, the player may jump the block; do not reduce the track width in response.

## 7. Difficulty progression and crowd-loss budgets

Increase difficulty in this order:

1. Pair a static lane hazard with a readable pulse signal.
2. Reveal a hidden trap before a moving saw.
3. Pair a rising block with a saw while preserving a lane or jump solution.
4. Combine overhead timing with shutter rhythm.
5. Add one directed bolt after the player has completed a moving-threat decision.
6. Teach the mandatory fracture jump in isolation.
7. Chain the fracture with a follow-up moving threat only in the final section.
8. Reduce recovery space only in the final 20%; do not shorten every warning at once.

| Section | Reliable safe routes | Hazard count | Primary skill | Difficulty |
|---|---:|---:|---|---|
| Crosswind Pulse Weave | 2–3 | 9 | Lane plus pulse timing | Medium |
| Drop-Saw Relay | 2–3 | 6 | Hidden reveal plus moving dodge | Medium-high |
| Block-Saw Interlock | 2 | 6–7 | Jump/lane interlock | High |
| Pendulum Shutter Exchange | 2 | 5 | Timing plus lane rhythm | High |
| Fracture Tutorial | 2, then 1 required jump | 3 | Jump contract | Very high |
| Final Fracture Relay | 1–2 at each event, never zero | 9–10 | Full relay | Very high |

Initial logical crowd-loss targets:

- Tutorial sections: 5–15% for a competent route.
- Block-Saw Interlock and Pendulum Shutter Exchange: 10–25%.
- Final relay: 15–35%.
- A single hazard activation must not remove more than 35% of the logical crowd.
- A successful jump over a barrier or beam removes zero logical runners. A full-clear fracture jump also removes zero; an early-release lead success may still reduce the logical count only through visual runners that fail the far-edge geometry.
- Physical fracture loss is geometry-driven: do not apply `failedLossCap`, `failedLossPercent`, random loss, or a percentage penalty to `usePhysicalGap`. Those serialized loss fields remain only for the legacy non-physical fallback; tune the authored gap, formation depth, and jump window if the measured fracture loss is too high.
- If a single event exceeds its budget, reduce its loss cap or kill envelope before increasing route complexity.

## 8. Optional pickup and recovery placement

Use the existing Pickups parent and currency path. Pickups are optional and must not hide a warning or sit on a predicted movement line.

| Z | Placement | Purpose |
|---:|---|---|
| 36 | x = 0 | Gate A recovery |
| 136 | x = -2.5 and +2.5 | Reward after Drop-Saw Relay |
| 208 | x = 0 | Calm beat before Pendulum Shutter Exchange |
| 270 | x = -2.5 and +2.5 | Recovery before the fracture tutorial |
| 312 | x = 0 | Reward after the first mandatory jump |
| 328 | x = -2.5 and +2.5 | Gate C recovery |
| 404 | x = -2, 0, +2 | Finish approach reward; no hazard overlap |

Do not place pickups inside pulse kill radii, under hammer heads, on the crack visual, inside the LeadStopper, at a shuttle endpoint, or on a cannon's predicted line.

## 9. Reusable prefab strategy

Reuse existing Level 2–4 prefab and component families where they are already verified. Do not copy inactive production hazards into the Level 5 route.

Preferred new folder: Assets/Prefabs/Traps/Level5/.

Create only the new family and its variants:

| Prefab | Purpose |
|---|---|
| CrackedSpanBase.prefab | Visual crack, edge markers, cached hazard references, and reset state |
| CrackedSpan_Short.prefab | 4.0m tutorial span |
| CrackedSpan_Final.prefab | Final span with stronger presentation but the same crossing contract |
| CrackTelegraph.prefab | Shared low-cost approach marker and edge lamp presentation |
| CrackLeadStopper.prefab | Player-only collision proxy with no crowd physics |

Shared CrackedSpanBase children:

- Visual: shared low-poly road-edge and crack meshes.
- Telegraph: approach marker, edge strips, and optional short audio cue.
- LeadStopper: player-only collision proxy.
- Bridge: disabled compatibility child; it must never provide the Level 5 crossing surface.
- HitVolume: logical crossing configuration; it must not process visual crowd colliders.

Use Level 4 prefab variants for JumpBarrierHazard, RisingBlockHazard, SweepBeamHazard, and ShutterBlockHazard where available. Use scene-authored instances of ConeHazard, FallingHazard, CircularSaw, PulsePlateHazard, SpikeSweepHazard, and SwingHammerHazard. Reuse ProjectileLauncher and TrackingProjectile for the limited cannons.

Keep runtime state on scene instances. Promote shared values to a focused ScriptableObject only if Level 5 and another scene demonstrate the need. Do not create a generic hazard manager or level-data framework for this scene alone.

## 10. Scene construction plan

When implementation is approved:

1. Duplicate the verified Assets/Scenes/Level4.unity to Assets/Scenes/Level5.unity and verify that the copy opens before changing content.
2. Preserve Level4.unity as a workbench. Do not modify its production obstacle instances while constructing Level 5.
3. Rename the main root to Level 5 - Fracture Relay.
4. Replace the track with the 10m x 420m track and verify player start, progress references, camera look-ahead, and finish at z = 418.
5. Remove Level 4 production trap instances from the playable route. Reuse only the planned authorities and explicitly placed instances.
6. Create these organized parents:

   Systems

   Track

   Gates

   Trap Sections/Crosswind Pulse Weave

   Trap Sections/Drop-Saw Relay

   Trap Sections/Block-Saw Interlock

   Trap Sections/Pendulum Shutter Exchange

   Trap Sections/Fracture Tutorial

   Trap Sections/Final Fracture Relay

   Pickups

   Lighting

   FinishGate

7. Verify the Level 4 jump input/state contract before creating the first CrackedSpan instance.
8. Create and isolate-test CrackedSpanBase, including disabled compatibility geometry, successful full-crowd jump, lead-success/partial-crowd jump, failed crossing, pause, and restart.
9. Build Crosswind Pulse Weave and Drop-Saw Relay without adding unplanned hazards.
10. Build Block-Saw Interlock and confirm that both the lane route and jump route remain valid.
11. Build Pendulum Shutter Exchange. Add the cannon only after the shuttle and shutter routes pass independently.
12. Build Fracture Tutorial with one fracture before creating the final fracture.
13. Add the three gates at z = 30, 150, and 320 using the existing Gate and Door setup.
14. Build Final Fracture Relay from the coordinate table. Do not add extra hazards to compensate for a failed balance pass.
15. Add optional pickups only after warning readability and crowd-loss budgets pass.
16. Update Level 5 HUD, world label, camera/Cinemachine names, and finish copy without adding a third UI controller.
17. Add Level5.unity to Build Settings only after the scene is playable and the approved scene order is known. Creating the scene does not include it in the build.

## 11. Script and architecture boundary

Keep the implementation thin and explicit:

- PlayerController.cs owns lead movement, jump buffering, coyote time, vertical velocity, and the read-only jump state used by jump hazards.
- PlayerCrowdManager.cs remains the owner of logical count, visual clone reuse, spiral placement, runner loss, pooled visual gap falls, and game-over checks. It does not simulate individual crowd jumps.
- CrackedSpanHazard.cs owns fracture presentation, physical-gap bounds, lead-only crossing evaluation, split-crowd transfer calls, and reset state. Its legacy Bridge/LeadStopper children remain disabled compatibility geometry.
- Existing hazard scripts own only their local state, warning, crossing evaluation, and bounded crowd-removal call.
- ProjectileLauncher.cs and TrackingProjectile.cs remain the only projectile authorities.
- Gate.cs and Door.cs remain the only gate authorities.
- LevelHud.cs remains the current HUD owner. UIManager.cs remains compatibility-only; do not add Level5Hud, TrapManager, or another global UI controller.

Communication and safety rules:

- Hazards may call the existing narrow crowd-removal API, but must not directly update HUD text, currency, pause state, or finish state.
- Cache PlayerController and PlayerCrowdManager references. Do not add repeated Find calls inside Update, hazard timers, or crossing checks.
- Prefer one crossing check per hazard event or movement leg over continuous per-runner physics.
- Use one authoritative normalized timer per moving obstacle. Pause freezes timers and positions; restart restores authored states.
- Add OnValidate clamps for span length, stopper height, warning lead, kill radius, phase, endpoint limits, loss caps, and any pool size.
- Guard against repeated loss on the same discharge, drop, saw event, shuttle leg, hammer cycle, shutter crossing, beam leg, barrier crossing, or fracture attempt.
- Keep physical road collision restricted to the authored road segments and lead CharacterController. Visual runners remain collider-free and use a logical gap transfer check.

## 12. Speed and movement assumptions

Use the existing 6–10m/s distance ramp as a Level 5 scene-specific configuration:

| Player Z | Target forward speed |
|---:|---:|
| 3 | 6.0m/s |
| 108 | 7.0m/s |
| 213 | 8.0m/s |
| 318 | 9.0m/s |
| 402 | 9.9m/s |
| 418 | 10.0m/s |

Initial formula:

progress = clamp01((playerZ - 3) / (418 - 3))
normalSpeed = lerp(6.0, 10.0, progress)

Configure the ramp on the Level 5 Player instance. Keep Level 1–4 scene values unchanged. Keep horizontal steering at the existing value until the block-saw and fracture route tests are complete. Do not increase horizontal speed, jump force, and hazard speed in the same tuning pass.

The fracture contract is based on the measured jump state, not a fixed player speed. Recheck the 4.0m span at 6m/s and 10m/s. The spike shuttle must retain its exact 3.00s endpoint travel regardless of the Level 5 speed ramp.

## 13. WebGL and runtime constraints

- Keep all Level 5 hazards scene-authored except bounded pooled cannon projectiles.
- Do not add per-runner colliders, Rigidbody components, or jump simulations.
- Keep the visual crack and telegraph low-poly, shared-material, and free of large particle bursts.
- Avoid gameplay-time Instantiate, Destroy, LINQ, repeated scene searches, and per-frame string formatting in new code.
- Use at most one bounded crowd scan per hazard crossing, pulse discharge, moving leg, or fracture attempt.
- Keep no more than two hazards performing hit checks in the same frame. Warnings may overlap visually, but evaluations must be staggered.
- Keep the Level 5 global projectile pool at or below 24 and no more than one immediate-window bolt in the planned route.
- Keep moving-hazard counts bounded; do not run two saws, two shuttles, or two beams as a simultaneous wall.
- The road is physically segmented at both authored fractures; do not reintroduce a bridge collider or add per-runner physics to simplify the crossing.
- Reuse simple URP-compatible materials and avoid unique materials for every trap instance.
- Do not enable WebGL threads, decompression fallback, or a larger memory heap to solve a Level 5 issue.
- Profile small, normal, and compressed-large crowds for active visual runners, crowd-removal calls, hazard checks, active projectiles, frame time, and GC allocations.

## 14. Playtesting gates

Before accepting Level 5:

- The scene opens and restarts cleanly with no missing script, prefab, input, material, or serialized references.
- The Level 5 HUD, world label, pause screen, restart state, and finish overlay show Level 5 - FRACTURE RELAY; no previous-level copy appears.
- All eleven prior hazard families are present in the playable route and identifiable: cones, falling traps, saws, pulse plates, spike shuttles, hammers, cannons, barriers, rising blocks, sweep beams, and shutters.
- The first pulse/cone section leaves two or more reliable lanes and the pulse charge signal is visible before discharge.
- Falling traps are invisible before their lane-independent trigger, activate once, and do not reveal each other.
- The block-saw interlock has a valid lane route and a valid jump route; it never becomes an unavoidable wall.
- Hammers, shutters, and spike shuttles preserve the prior timing contracts. Every spike endpoint-to-endpoint leg is exactly 3.00s ± 0.05s, the endpoint dwell is 0.75s ± 0.05s, and the full cycle is 7.50s ± 0.1s.
- Cannon bolts use the existing pool, show a charge cue, and never create an overlapping full-crowd wipe.
- The first fracture is visibly understandable. A grounded lead player is stopped at the fracture and cannot continue until jumping.
- A full-hold jump over both fractures clears the measured span at 6m/s and 10m/s where formation geometry permits, with no fracture-related logical loss; an early release can preserve the lead/front and drop only trailing FAILED runners.
- Each runner crossing is resolved once from its geometry and shared trajectory; the physical gap does not apply an arbitrary percentage or retry penalty.
- No visual runner creates crack, obstacle, or trigger collision traffic.
- Both left and right choices at all three gates can complete the level.
- The final 14m before the finish marker contains no new hazard activation, projectile launch, or crowd-removal check.
- The finish trigger and result presentation remain clear after the final shuttle.
- No new recurring gameplay GC spikes, console errors, physics-trigger floods, or unbounded clone growth appear.
- Test at 960 x 600 and a narrow/mobile-like aspect ratio.
- Test a Chromium WebGL build and a lower-powered browser profile when the build is available.

## 15. Route test matrix

| Test route | Input intent | Expected result |
|---|---|---|
| Left-first | Take left at all gates; favor left/outer routes | Completes with moderate crowd loss |
| Right-first | Take right at all gates; favor right/outer routes | Completes with moderate crowd loss |
| Alternating | Alternate gate sides and reverse around cone, saw, hammer, and shutter events | Completes; validates route independence |
| Block-saw lane route | Avoid the rising block, then respond to the saw endpoint | Completes without requiring a jump |
| Block-saw jump route | Jump the block and land before the pulse row | Completes; validates vertical/lateral handoff |
| Fracture jump route | Jump both fractures at 6m/s and 10m/s | Lead passes; no logical loss from successful crossings |
| Mixed | Combine steering, selective jumps, and one cannon dodge | Completes with readable causes for losses |
| Hold-center negative | Avoid steering and jumping | Expected losses; not evidence that the level is unbeatable |
| Large-crowd route | Enter the final relay with the highest safe logical count | No instant full wipe, clone explosion, or frame spike |
| Pause/restart | Pause during pulse, falling, saw, shuttle, beam, shutter, and fracture states, then restart | Timers freeze; restart restores authored states |
| Browser input | Test mouse drag, keyboard steering/Space, and supported mobile/browser jump binding | No absent-device early return disables the other supported input |

## 16. Implementation order and verification

1. Duplicate Level4.unity to Level5.unity and verify the copy opens before removing or replacing content.
2. Set the root, track length, gates, finish, camera look-ahead, and organized parent hierarchy.
3. Audit the inherited Level 2–4 hazard scripts and prefab references. Confirm the current Level 4 jump-state API is available.
4. Build CrackedSpanBase in an isolated test area. Verify disabled compatibility geometry, segmented-road jump clearance, full/partial/no-jump outcomes, pooled fall transfer, pause, and restart.
5. Build Crosswind Pulse Weave and verify cone-plus-pulse readability with a small crowd.
6. Build Drop-Saw Relay and verify hidden activation, one-shot reset, saw movement, and lane-independent triggers.
7. Build Block-Saw Interlock with one rising block and one saw first. Measure the lane and jump routes before adding the second block and pulse row.
8. Build Pendulum Shutter Exchange. Add the spike shuttle and cannon only after hammer and shutter routes pass independently.
9. Build Fracture Tutorial and confirm a grounded player cannot continue across the first fracture.
10. Add Gate B and Gate C route checks before constructing the final relay.
11. Build Final Fracture Relay directly from the coordinate table. Do not add unplanned hazards during the first balance pass.
12. Add optional pickups and final presentation only after hazard warnings and crowd-loss budgets pass.
13. Run left-first, right-first, alternating, block-saw lane, block-saw jump, fracture, pause/restart, large-crowd, and browser-input tests.
14. Profile editor and WebGL behaviour at small, normal, and compressed-large crowd sizes.
15. Only after scene acceptance, review the Build Settings scene order. Do not assume Level5.unity is included.

## 17. Review decisions before implementation

Proposed defaults for approval:

- 420m straight track.
- Scene and player-facing name: Level 5 - Fracture Relay.
- Gates at z = 30, 150, and 320 with +45/x2, +75/x2, and +110/x2.
- Every Level 2–4 hazard family appears in the production route.
- New CrackedSpanHazard uses a visual full-width fracture over a real 4.0m segmented-road gap; legacy LeadStopper and Bridge children are disabled.
- The player must jump to continue across both fractures; there is no lane bypass.
- Releasing jump early produces a shorter arc; visual runners left inside a gap fall while runners already on the far segment remain active.
- Level 5 uses the existing 6–10m/s ramp, Level 4 jump contract, and exact Level 3 spike timing contract.
- Level 5 uses no new UI authority and no new projectile pool.
- New crack prefabs live under Assets/Prefabs/Traps/Level5/.
- Level5.unity remains out of Build Settings until scene acceptance and scene-order approval.

Confirm separately:

- Whether the 420m run length feels appropriate after the first graybox timing pass.
- Whether the 4.0m physical gap has enough jump margin at both ends of the 6–10m/s speed ramp.
- Whether the recessed pit visual reads clearly without an under-gap collider or recovery plane.
- Whether the final spike shuttle warning should activate 18m or 24m before its root after the 960 x 600 warning-load test.
- Whether the planned one-bolt cannon pressure improves the final chain or should be removed if it obscures the fracture cue.
- Whether Level 5 should follow Level 4 in Build Settings or remain a workbench scene.

## 18. Acceptance record template

Do not mark this plan implemented until measured results replace the TBD values.

| Check | Result | Evidence / file |
|---|---|---|
| Fracture span length at tutorial/final | TBD | CrackedSpanHazard configuration and route test |
| Jump apex and airborne duration | TBD | PlayerController probe or timing test |
| Fracture clearance at 6m/s | TBD | Route test |
| Fracture clearance at 10m/s | TBD | Route test |
| Grounded fracture fall/game-over behaviour | TBD | Playtest log |
| Logical loss for visual runners falling through a fracture | TBD | Playtest log |
| Early jump release splits front and rear crowd | TBD | Playtest log |
| Block-saw lane route | TBD | Route test |
| Block-saw jump route | TBD | Route test |
| Pulse maximum logical loss | TBD | Playtest log |
| Falling trap activation positions | TBD | Playtest log |
| Saw movement and loss cap | TBD | Timing/playtest log |
| Spike outbound and return travel | TBD | Timing test |
| Spike cycle duration | TBD | Timing test |
| Hammer/shutter route timing | TBD | Playtest log |
| Maximum active projectiles | TBD | Profiler/editor observation |
| Small/normal/large crowd frame time and GC | TBD | Profiler capture |
| 960 x 600 HUD and jump-control check | TBD | Screenshot/browser note |
| Narrow/mobile-like HUD and jump-control check | TBD | Screenshot/browser note |
| Chromium WebGL pass | TBD | Build/browser test note |
| Lower-powered browser pass | TBD | Build/browser test note |
| Build Settings scene order | TBD | ProjectSettings/EditorBuildSettings.asset |
