# Level 3 Implementation Plan - Pulsebound Foundry

This document is the proposed design reference for constructing Level 3. It is a planning document only; scene, script, prefab, and ProjectSettings changes must be implemented separately and verified against this plan.

Level 3 should feel like a new skill test rather than a longer copy of Level 2. Level 2 teaches cones, hidden falling traps, and moving saws. Level 3 replaces those hazards with timed floor pulses, moving spike shuttles, swinging hammers, and telegraphed side-cannon projectiles. The player's main challenge is reading timing and committing to lanes while the spiral crowd compresses and expands.

## 1. Design goals

- Give Level 3 a distinct identity: a bright, mechanical pulse foundry that tests rhythm, timing, and lane commitment.
- Use a different active hazard set from Level 2. Do not place `ConeHazard`, `FallingHazard`, or `CircularSaw` instances in the production Level 3 route.
- Teach each new trap in isolation before combining it with another trap.
- Make the spike shuttle a predictable, learnable signature hazard. Every endpoint-to-endpoint movement leg must take exactly 3.00 seconds.
- Keep at least two technically safe lanes through the first 75% of the run. Allow one-to-two safe lanes only in the final combination, and never create an unavoidable full-width wall.
- Increase difficulty through timing overlap, phase offsets, and reduced recovery space rather than uncontrolled object counts or instant crowd wipes.
- Preserve the Fermat/golden-angle spiral formation and make its width part of the decisions: a route that is safe for the player center may still be unsafe for the crowd edge.
- Reuse the current gate, door, crowd, pooling, and projectile infrastructure where it is appropriate. Add small, explicit hazard behaviours only where the existing scripts do not match the new trap rules.
- Stay within the project's WebGL constraints: scene-authored hazards, bounded pools, no per-frame allocations, and no native-only dependencies.

## 2. Player experience and trap vocabulary

The player-facing level name is **Level 3 - Pulsebound Foundry**. The route is built around four original trap families:

| Trap family | Player read | Main skill | Initial implementation boundary |
|---|---|---|---|
| Pulse plates | Floor rings charge, flash, then discharge | Rhythm and lane timing | New `PulsePlateHazard.cs` |
| Spike shuttles | A compact spiked carriage sweeps between two visible endpoints | Tracking a moving threat | New `SpikeSweepHazard.cs` |
| Swing hammers | Overhead arms swing through a marked lane | Timing a crossing | New `SwingHammerHazard.cs` |
| Side-cannon bolts | A turret charges, points, and fires one readable homing bolt | Short lateral dodge | Existing `ProjectileLauncher.cs` plus a bounded warning/telegraph change if required |

The spike shuttle is a moving obstacle, not a solid spike wall. It occupies approximately one lane at a time and leaves a readable route around it. Its exact motion contract is:

- Start endpoint: x = -3.8m.
- End endpoint: x = +3.8m.
- Forward leg: start endpoint to end endpoint in exactly 3.00 seconds.
- Return leg: end endpoint to start endpoint in exactly 3.00 seconds.
- Endpoint dwell: 0.75 seconds at each endpoint.
- Full start-to-start cycle: 7.50 seconds.
- Motion is driven by normalized elapsed time and `Lerp`, not accumulated velocity, so frame rate does not change the 3-second travel time.
- The carriage becomes active only when the player reaches its approach line. Once active, it repeats its cycle until the player has passed the section.

This means both left-to-right and right-to-left sweeps travel their full range in three seconds. The requirement is a gameplay invariant and must have a focused timing check before scene acceptance.

## 3. Coordinate convention

Use the same convention as Level 1 and Level 2:

- Track center: x = 0.
- Track surface: y = 0.
- Forward direction: positive z.
- Track width: 10m.
- Recommended lane centers: x = -3, -1, 1, 3.
- Approximate track boundary: x = -5 to x = +5.
- Proposed Level 3 length: 340m.
- Track position: (0, -0.1, 170).
- Player start: approximately (0, 0, 3).
- Finish area: z = 332-338.

All coordinates below are world coordinates when the Level 3 root is at (0, 0, 0). If the root is moved, apply the root offset to every position.

Avoid placing lethal hazards closer than 1.2m to the outer boundary. The crowd system can remove runners near the edge, so a visual lane that touches x = +/-5 is not a reliable survival lane. Keep all authored kill centers at |x| <= 3.8 unless a specific hazard is an overhead or side-mounted visual that cannot kill at its mount point.

The track remains straight. Do not introduce curves, physical track narrowing, or segmented boundary rules in this level; timing and lane changes are the intended difficulty.

## 4. Level flow

| Section | Z range | Purpose |
|---|---:|---|
| Start runway | 0-20 | Establish the foundry palette, crowd spacing, and forward camera read |
| Gate A | 28 | First reward decision before any timed trap |
| Pulse Plate Tutorial | 42-66 | Teach the charge-flash-discharge rhythm |
| Spike Shuttle Tutorial | 78-112 | Introduce the signature 3-second endpoint sweep |
| Gate B | 128 | Reward the first timing lesson and create a count difference |
| Hammer Alley | 144-178 | Teach overhead swing timing in alternating lanes |
| Cannon Walkway | 190-222 | Teach charge warning and short lateral dodges |
| Gate C | 240 | Last major growth/economy choice before combinations |
| Foundry Exchange | 254-286 | Combine pulse plates, one spike shuttle, and staggered hammers |
| Final Pulse Lock | 296-326 | Highest difficulty; combine all four trap families with recovery beats |
| Finish buffer | 332-340 | Clear approach, result presentation, and finish reward |

The run should be approximately 340m, but the exact length is secondary to preserving the section spacing and reaction windows. If playtesting shows that the speed ramp makes the final sequence unreadable, move a section boundary rather than making the hazards faster without a design review.

## 5. Shared track coordinates

| Object | Position / size |
|---|---|
| Track | Position (0, -0.1, 170); size 10 x 0.2 x 340 |
| Start line | (0, 0.01, 3) |
| Player start | (0, 0, 3) |
| Gate A root | (0, 0, 28) |
| Gate B root | (0, 0, 128) |
| Gate C root | (0, 0, 240) |
| Finish marker | (0, 0.02, 335) |
| Finish gate | (0, 0, 338) |
| Camera look-ahead | Approximately 10-14m; review during the fastest section |

Keep at least 7m of clear track after each gate before the first hazard. A gate trigger must never overlap a pulse plate, hammer kill volume, projectile launch position, or spike shuttle endpoint.

## 6. Gate plan

The gates are pacing tools and remain readable `+N` versus `xN` choices. Use the existing `Gate.cs` and `Door.cs` behaviour unless the integer-only gate value is deliberately changed in a separate gameplay-math task.

### Gate A - first pulse commitment

Position: Gate A root at (0, 0, 28).

Values:

- Left: +30.
- Right: x2.

Make the gate readable from approximately z = 21. Leave z = 35-41 clear. The first pulse plate is not allowed to activate before the player has had time to finish the gate choice and recenter if needed.

### Gate B - timing reward

Position: Gate B root at (0, 0, 128).

Values:

- Left: +50.
- Right: x2.

Make the gate readable from approximately z = 118. Leave z = 135-143 clear so the player can see the first hammer warning. Do not use x3 in the first implementation pass; keeping the maximum count controlled is more valuable than a larger multiplier while the new hazards are being tuned.

### Gate C - final growth decision

Position: Gate C root at (0, 0, 240).

Values:

- Left: +75.
- Right: x2.

Make the gate readable from approximately z = 230. Leave z = 247-253 clear. The first combined pulse warning must be visible after the decision, and neither side of the gate may be blocked by a cannon mount.

The selected gate side must not determine whether the player can complete the level. It may change the crowd width and therefore the difficulty of the same trap pattern, but both left and right choices need a survival route.

## 7. Trap specification and coordinate sheet

### 7.1 Pulse Plate Tutorial

Pulse plates are small floor-mounted discs. A plate cycles through `Idle -> Charging -> Discharge -> Recovery`, with a visible ring and a short sound cue for `Charging`. The active discharge should be brief enough that a player who reacts to the flash can cross safely.

Initial settings:

- plate radius: 0.95m.
- kill radius: 1.05m.
- charge warning: 0.75s.
- discharge duration: 0.35s.
- recovery duration: 1.35s.
- full cycle: 2.45s.
- maximum runner loss per discharge: 2 logical runners per plate.
- pulse damage is checked once during the active window, not continuously every frame.
- boundaryX remains 5.0m; plates do not replace the crowd boundary safety logic.

Coordinate pattern:

| Z | Plate X positions | Design intent |
|---:|---|---|
| 44 | 0 | One obvious center threat; three broad routes remain |
| 51 | -2.4 | Introduce a safe center/right choice |
| 58 | 2.4 | Require a small direction change |
| 65 | -1.2, 1.2 | Two center plates leave the outer lanes open |

The first plate should be visibly charging at least 8m before the player reaches its Z coordinate. Do not make all four plates fire in phase. Use phases 0.0s, 0.55s, 1.10s, and 1.65s so the player learns a rhythm without receiving one large simultaneous hit test.

### 7.2 Spike Shuttle Tutorial

The spike shuttle is the Level 3 signature. It is a compact spiked carriage moving along a visible horizontal rail. The carriage should be approximately 1.7m wide and should not span the full track.

| Object | Position / authored motion |
|---|---|
| Rail 1 / Spike Shuttle 1 | root at (0, 0.55, 88); x endpoints -3.8 and +3.8 |
| Rail warning line | z = 64; visible approach marker begins at z = 60 |
| Rail 2 / Spike Shuttle 2 | root at (0, 0.55, 106); x endpoints +3.8 and -3.8 |
| Rail 2 warning line | z = 82; phase offset = 3.75s |

Use these initial settings:

- startX / endX: -3.8 / +3.8 for Shuttle 1; +3.8 / -3.8 for Shuttle 2.
- travelDuration: exactly 3.0s.
- endpointPause: 0.75s.
- cycle duration: 7.5s.
- carriage kill radius: 0.65m horizontally, with a vertical hit range of approximately 0.8m.
- maximum runner loss per directional leg: 4 logical runners.
- approach activation distance: 24m before the rail Z.
- movement easing: linear for the first pass; do not use an ease-in curve because the player must learn the three-second timing.
- phase offset: 0s for Shuttle 1 and 3.75s for Shuttle 2.

The phase offset is not permission to make the two shuttles overlap into a wall. The player should encounter Shuttle 1, receive at least 7m of recovery space, and then read Shuttle 2 as a separate timing event. If both are visible at once, they must be separated by at least 12m in Z.

Required motion state machine:

```text
Dormant
    player crosses approach Z
        enter AtStart and begin the authored cycle
AtStart
    wait endpointPause
    enter SweepingToEnd
SweepingToEnd
    t = clamp01(elapsed / 3.0)
    x = lerp(startX, endX, t)
    when t == 1: enter AtEnd
AtEnd
    wait endpointPause
    enter SweepingToStart
SweepingToStart
    t = clamp01(elapsed / 3.0)
    x = lerp(endX, startX, t)
    when t == 1: enter AtStart
```

Pause state must stop the state timer and preserve the carriage position. Restarting the scene resets the shuttle to its authored start endpoint and phase. A moving carriage should not use a Rigidbody or depend on physics frame rate for its route.

The warning presentation should include endpoint lamps, a short rail glow, and an optional simple tick sound. It must show the two endpoints and the direction of the next sweep. Keep the warning as scene/prefab presentation; do not add a global hazard UI manager.

### 7.3 Hammer Alley

Hammers are overhead pendulums that sweep through a marked lane. Each hammer occupies one lane at a time, and the alternate lanes remain clear. The danger must be legible from the arm shadow/mesh and a painted floor arc.

| Z | Pivot X | Initial lane focus | Notes |
|---:|---:|---|---|
| 148 | -2.4 | left/center | First single hammer; three-lane recovery |
| 160 | 2.4 | right/center | Alternates direction |
| 172 | 0 | center | Requires choosing an outer lane |

Initial settings:

- pivot height: 3.2m.
- arm length: 2.0m.
- swing angle: -55 to +55 degrees around the authored forward-facing pivot.
- full oscillation: 2.6s.
- warning lead: 0.8s before the player enters the hammer's 16m approach zone.
- kill radius: 0.8m around the hammer head.
- maximum runner loss per swing cycle: 3 logical runners.
- phase offsets: 0.0s, 0.85s, and 1.7s.

Do not put two hammer heads at the same Z. The floor arc should communicate the full horizontal reach, but it must not paint the entire 10m track as dangerous. If a visual hammer needs a longer arm for readability, reduce its kill radius rather than shrinking the safe route.

### 7.4 Cannon Walkway

Side cannons create short, directed pressure without becoming a continuous projectile wall. Reuse the existing pooled `ProjectileLauncher` and `TrackingProjectile` path. A turret should fire only after the player enters its trigger range and should target the nearest active runner as the current system does.

| Object | Position | Initial side | Phase |
|---|---|---|---:|
| Cannon 1 | (-4.35, 1.4, 190) | left wall toward track | 0.0s |
| Cannon 2 | (4.35, 1.4, 202) | right wall toward track | 0.85s |
| Cannon 3 | (-4.35, 1.4, 214) | left wall toward track | 1.7s |
| Cannon 4 | (4.35, 1.4, 222) | right wall toward track | 2.55s |

Initial launcher settings:

- poolSize: 8 per cannon, with a global authored cap of 24 Level 3 projectiles.
- fireInterval: 2.5s.
- triggerRadius: 18m.
- projectile speed: 8.5-9.0m/s.
- projectile kill radius: 0.6m.
- projectile lifetime: 4.0s.
- no more than two active bolts in the same 18m player window during the first pass.
- stagger initial fire timers using the phase table; do not let all cannons fire on `Start`.

Before firing, a cannon should flash or project a short aiming line for approximately 0.35s. If adding this cue to `ProjectileLauncher.cs` would affect Level 1, use a serialized child telegraph or a Level 3-only presentation component. Do not duplicate the projectile pool or create a second launcher authority.

The projectile is a dodge test, not a crowd wipe. If tests show that a homing bolt curves into the entire spiral, reduce `steerFactor` or the kill radius before reducing the player's horizontal control. A missed bolt should expire in the pool without runtime destruction/recreation.

### 7.5 Foundry Exchange combination

This is the first combined section. Keep enough separation that the player can identify cause and effect after a loss.

| Z | Hazard | Placement |
|---:|---|---|
| 254 | Pulse row | x = -2.4, +2.4; phase 0.0s / 1.0s |
| 261 | Spike Shuttle 3 | x endpoints -3.8 / +3.8; root at z = 270; starts left |
| 270 | Hammer pair | pivots at x = -2.4 and +2.4; phase offset 1.3s |
| 278 | Pulse row | x = -1.2, 0, +1.2; only the outer lanes should remain comfortable |
| 286 | Recovery marker | no lethal object; collectable/presentation space only |

For the third shuttle, its rail root is z = 270 and its forward approach line is z = 246. The first pulse row must not hide the shuttle's endpoint lamps. If the crowd is large, move the center pulse at z = 278 to x = +/-1.4 rather than reducing the track width.

### 7.6 Final Pulse Lock

The final section tests all four trap families but uses recovery beats so that failure is attributable. It must feel like a climax, not an unreadable object pile.

| Z | Hazard | Placement / phase |
|---:|---|---|
| 296 | Pulse row 1 | x = -2.4, +2.4; phases 0.0s / 0.8s |
| 304 | Spike Shuttle 4 | root at z = 312; endpoints +3.8 / -3.8; starts right |
| 312 | Hammer | pivot x = 0; phase 1.25s |
| 319 | Cannon pair | left x = -4.35 and right x = +4.35; only one may fire per 2.5s window |
| 326 | Pulse row 2 | x = -1.4, +1.4; phase 1.0s |
| 330 | Recovery runway | completely clear |
| 335 | Finish marker | clear |
| 338 | Finish gate | clear |

The fourth shuttle's approach line is z = 288. Its endpoint lamps must be visible before the z = 296 pulse row becomes active. The final cannon pair should not both target the same runner in the same frame. The final 8m before the finish marker is always clear so that crowd loss, finish detection, and the result UI are not visually or physically combined.

## 8. Difficulty progression

Increase difficulty in this order:

1. Teach a single timing signal with a broad lateral route.
2. Alternate the safe lane between rows.
3. Add phase offsets while preserving one obvious next action.
4. Reduce recovery space between different trap families.
5. Combine a timing trap with a moving lane threat.
6. Increase the number of active warnings only in the final 20%.

Do not simultaneously shorten warning time, increase trap speed, reduce safe lanes, and increase crowd loss. If a section is too easy, change one variable and record the measured result.

| Section | Safe lanes | Hazard count | Timing complexity | Difficulty |
|---|---:|---:|---|---|
| Pulse Plate Tutorial | 3-4 | 6 plates | One readable phase | Medium |
| Spike Shuttle Tutorial | 2-3 | 2 shuttles | 3s directional legs, 7.5s cycles | Medium-high |
| Hammer Alley | 2-3 | 3 hammers | Alternating lane focus | High |
| Cannon Walkway | 2-3 | 4 cannons | Staggered charge and bolts | High |
| Foundry Exchange | 2 | 9-11 hazards | Cross-family timing | Very high |
| Final Pulse Lock | 1-2 | 10-12 hazards | All trap families plus recovery | Very high |

Initial expected crowd-loss budget is 5-15% in the tutorial sections, 10-25% in Hammer Alley and Cannon Walkway, and 15-35% in the final combination for a competent route. If a single hazard removes more than 35% of the logical crowd in one activation, reduce its loss cap or kill radius before changing the route.

## 9. Optional pickup and recovery placement

Pickups should reinforce route reading, not obscure hazard telegraphs. Use the existing `Pickups` parent and currency path.

Proposed placements:

| Z | Placement | Purpose |
|---:|---|---|
| 36 | x = 0 | Small reward after Gate A, before the first pulse |
| 116 | x = -2.5 and +2.5 | Choice reward after the shuttle tutorial |
| 182 | x = 0 | Calm beat before cannon fire |
| 228 | x = -2.5, +2.5 | Recovery after Cannon Walkway |
| 288 | x = 0 | Final recovery marker before the last lock |
| 330 | x = -2, 0, +2 | Finish approach reward; no hazard overlap |

Do not place currency directly on an endpoint, inside a pulse kill radius, under a hammer head, or on the predicted projectile line. If pickup collision is not bounded by the current implementation, omit the optional pickups from the first scene pass.

## 10. Scene construction plan

When implementation is approved:

1. Duplicate the verified Level 2 scene to `Assets/Scenes/Level3.unity` so Player, camera, EventSystem, HUD, wallet, crowd pool, finish UI, and input wiring remain consistent.
2. Preserve the Level 2 scene as a workbench. Do not modify its trap instances while constructing Level 3.
3. Rename the main root to `Level 3 - Pulsebound Foundry`.
4. Replace the Level 2 track with the 10m x 340m track and move/verify the finish at z = 338.
5. Remove the Level 2 trap instances from the Level 3 copy. Do not leave inactive cones, falling traps, or saws in the production route unless they are explicitly marked as authoring references outside the playable hierarchy.
6. Create these organized parents:

    Systems

    Track

    Gates

    Trap Sections/Pulse Plate Tutorial

    Trap Sections/Spike Shuttle Tutorial

    Trap Sections/Hammer Alley

    Trap Sections/Cannon Walkway

    Trap Sections/Foundry Exchange

    Trap Sections/Final Pulse Lock

    Pickups

    Lighting

    FinishGate

7. Create or duplicate dedicated prefabs for pulse plates, spike shuttles, and swinging hammers. Keep the scripts on the hazard root and child meshes limited to visual presentation.
8. Reuse the pooled projectile launcher for cannons. Pre-warm bounded pools and verify inactive projectiles are not visible or counted by gameplay collision.
9. Add the three gates using the existing gate/door setup at z = 28, 128, and 240.
10. Set Level 3-specific player speed values only on the Level 3 Player instance. Keep Level 1 and Level 2 values unchanged.
11. Update Level 3 HUD, world label, camera/Cinemachine names, and finish copy without adding a third UI controller.
12. Add `Level3.unity` to Build Settings only after the scene is playable and the approved scene order is known. The current project constitution says Level 2 is not yet enabled, so do not silently change build order as part of this plan.

## 11. Script and architecture boundary

The project remains a small-game prototype. Keep the implementation thin and explicit:

- Scene/bootstrap layer: Level 3 scene hierarchy owns object placement, references, phase offsets, and level-specific serialized values.
- Crowd/domain layer: `PlayerCrowdManager.cs` remains the owner of logical runner count, visual clone reuse, spiral placement, runner removal, and game-over checks.
- Track hazard layer: `PulsePlateHazard.cs`, `SpikeSweepHazard.cs`, and `SwingHammerHazard.cs` own only their local state machine, warning state, and bounded runner-hit query.
- Existing projectile layer: `ProjectileLauncher.cs` owns the pooled launch schedule; `TrackingProjectile.cs` owns movement, lifetime, and return-to-pool behaviour.
- Gate layer: `Gate.cs` and `Door.cs` own the gate math and one-time side selection.
- Presentation layer: Level 3 HUD copy stays with `LevelHud`; the legacy `UIManager` remains compatibility-only. Do not add a Level3Hud or a global TrapManager.

Data ownership for the first pass:

- Per-instance coordinates, phases, warning lead, kill radius, and loss caps belong on serialized hazard components or prefabs.
- Shared values that need balancing across multiple levels should become ScriptableObjects only after two scenes demonstrate the need. Do not create a generic level-data framework for this scene alone.
- Runtime state such as current pulse phase, shuttle motion state, and active projectile ownership must remain on the runtime component, not in a shared asset.
- Pure timing helpers such as normalized shuttle interpolation and pulse phase evaluation should be plain C# methods or small testable functions where practical.

Communication rules:

- Hazards may call the narrow crowd-removal API already used by existing hazards, but must not directly update HUD text, currency, pause state, or finish state.
- Hazards should cache their `PlayerCrowdManager` reference and return early when the reference or game state is unavailable.
- Use direct serialized references for authored scene dependencies. Do not add repeated `Find*` calls inside per-frame loops.
- If a global event is needed for telegraph audio or analytics, keep it one-way and optional; gameplay correctness must not depend on the UI subscriber.

Required implementation safeguards:

- `SpikeSweepHazard` must use elapsed-time interpolation and a single authoritative state timer. Pause and restart behaviour must be explicit.
- Each pulse plate must have a one-shot hit guard per discharge so one visual runner does not trigger multiple losses during one flash.
- Hammer and shuttle losses need a per-cycle cap. A crowd crossing a kill volume must not be able to lose every runner in one frame.
- New serialized counts, pool sizes, radii, durations, and endpoints need `OnValidate` clamps that prevent negative durations, endpoints outside the road, and unbounded pools.
- The hazard scripts must not instantiate or destroy objects during gameplay. Visual warnings should be pooled or scene-authored.

## 12. Speed and movement assumptions

Level 3 should initially use the Level 2-style distance ramp so trap novelty is isolated from a second major speed change:

| Player Z | Target forward speed |
|---:|---:|
| 3 | 6.0m/s |
| 85 | 6.9m/s |
| 170 | 7.9m/s |
| 255 | 8.8m/s |
| 325 | 9.8m/s |
| 338 | 10.0m/s |

Initial formula:

```text
progress = clamp01((playerZ - 3) / (338 - 3))
normalSpeed = lerp(6.0, 10.0, progress)
```

Configure these values on the Level 3 Player instance rather than changing the Level 1 or Level 2 defaults. Keep horizontal steering at the existing starting value until playtesting shows that the three-second spike sweep cannot be dodged at the fastest section. If horizontal speed is changed, record it as a Level 3-specific decision and rerun the spike timing tests.

The spike timing contract is independent of player speed: player speed changes when the player meets the shuttle, but never changes the shuttle's 3.00-second endpoint travel time. The warning lead should be tuned against the fastest 10m/s case, not only the opening 6m/s case.

## 13. WebGL and runtime constraints

- Keep all Level 3 hazards scene-authored except for the already bounded pooled projectile objects.
- Use no per-frame `Instantiate`, `Destroy`, LINQ, string formatting, or repeated scene searches in the new hazard scripts.
- Prefer one bounded `ActiveRunners` scan per hazard activation/cycle over continuous per-runner physics.
- Pulse plates should check hits once per discharge; hammers and shuttles should use bounded cycle checks and a loss cap.
- Keep total scene-authored moving hazards at no more than two simultaneously active in the player's immediate 20m window during the tutorial and middle sections. The final section may show three moving warnings, but no more than two should perform hit checks in the same frame.
- Keep the global Level 3 projectile pool at or below 24 instances and verify the existing object pool returns every expired bolt.
- Reuse materials and simple URP-compatible shaders. Avoid a unique material per plate, spike, hammer, and projectile.
- Keep warning effects low cost: emissive color, simple mesh pulse, or a small pooled effect. Avoid full-screen post-processing and large particle bursts.
- Do not enable WebGL threads, decompression fallback, or a larger memory heap to solve Level 3 issues. Diagnose asset/code causes first.
- Profile small, normal, and compressed-large crowds. Record active visual runners, hazard checks, projectile count, GC allocations, and frame time.

## 14. Playtesting gates

Before accepting Level 3:

- The scene opens and plays from a clean restart with no missing script references.
- The Level 3 HUD and world label show `LEVEL 3 - PULSEBOUND FOUNDRY`; no Level 1 or Level 2 copy appears during play, pause, restart, or finish.
- Pulse plates show a clear charge signal at least 6-8m before discharge and leave a reliable route.
- A pulse discharge can remove runners but cannot unintentionally wipe the entire crowd in one activation.
- Each spike shuttle starts from its authored endpoint, reaches the opposite endpoint in exactly 3.00s +/- 0.05s in the editor timing test, and takes exactly 3.00s in the return direction.
- Spike endpoint lamps and direction cues are visible before the carriage enters the player's immediate reaction space.
- Spike shuttle pauses are 0.75s +/- 0.05s, the full cycle is 7.50s +/- 0.1s, and pause/restart preserves or resets state as specified.
- Shuttle 1 and Shuttle 2 do not combine into an unavoidable wall. A player who changes lane in response to the warning can survive both.
- Hammers read as overhead threats, and each hammer leaves at least two lanes available at its authored crossing.
- Cannon charge cues appear before a bolt is launched; no more than the planned number of active bolts overlaps the player's immediate window.
- Bolts use the existing pool and do not create recurring GC spikes or visible inactive objects.
- Both left and right choices at all three gates can complete the level.
- The finish trigger, recovery runway, and result presentation remain clear after the final pulse row.
- The logical crowd count, spiral formation, and visual clone cap remain bounded through the largest intended gate outcome.
- There are no new recurring gameplay GC spikes, console errors, or physics-trigger floods.
- The level is tested at 960 x 600 and a narrow/mobile-like aspect ratio.
- The scene is tested in a Chromium-based browser and a lower-powered browser profile when the WebGL build is available.

Route test matrix:

| Test route | Input intent | Expected result |
|---|---|---|
| Left-first | Take left at all gates; favor outer lanes in final section | Completes with moderate crowd loss |
| Right-first | Take right at all gates; favor the opposite outer lanes | Completes with moderate crowd loss |
| Alternating | Change side at each gate and alternate around pulses/hammers | Completes; validates recovery and gate independence |
| Hold-center negative test | Avoid steering except where impossible | Expected to lose runners; not evidence that the level is unbeatable |
| Large-crowd route | Enter final section with the highest safe test count | No unbounded clones, frame spike, or instant full wipe |
| Pause/restart | Pause during each shuttle state, then restart the scene | Timers freeze on pause; restart returns every trap to authored initial state |

## 15. Implementation order and verification

Implement the plan in this order:

1. Duplicate Level 2 to `Level3.unity` and verify the copy still opens before removing content.
2. Set the Level 3 root, track length, player start, finish, camera look-ahead, and organized parent hierarchy.
3. Create the Pulse Plate Tutorial and verify warning/discharge timing with a small crowd.
4. Implement `SpikeSweepHazard.cs` with a focused 3-second endpoint timing test before adding multiple instances.
5. Add the two tutorial shuttles, verify phase offsets, route readability, pause, and restart.
6. Create `SwingHammerHazard.cs` and build Hammer Alley with one hammer first, then add the alternating instances.
7. Reuse and tune the pooled projectile launcher for Cannon Walkway. Add the Level 3-only telegraph presentation if the existing launcher has no warning cue.
8. Add Gates B and C and verify that both sides produce valid routes before combining trap families.
9. Build Foundry Exchange and Final Pulse Lock from the coordinate tables. Do not add extra hazards during the first playtest pass.
10. Add optional pickups only after hazard readability and crowd-loss budgets pass.
11. Update Level 3 HUD/world/finish copy and verify Level 1 and Level 2 text remains unchanged.
12. Run left-first, right-first, alternating, pause/restart, and large-crowd route tests.
13. Profile editor and WebGL behaviour at small, normal, and compressed-large crowd sizes.
14. Only after scene acceptance, review Build Settings order. Do not assume creating `Level3.unity` makes it part of the build.

## 16. Review decisions before implementation

The following are proposed defaults for approval:

- 340m straight track.
- Scene name and player-facing name: `Level 3 - Pulsebound Foundry`.
- Gates at z = 28, 128, and 240 with `+30/x2`, `+50/x2`, and `+75/x2`.
- Active Level 3 trap families: pulse plates, spike shuttles, swinging hammers, and side-cannon bolts.
- Level 2's cones, hidden falling traps, and circular saws are excluded from the production Level 3 route.
- Every spike endpoint-to-endpoint leg is exactly 3.00 seconds, with a 7.50-second full cycle.
- The Level 3 speed ramp initially remains 6-10m/s to isolate the new trap set.
- Pickups are optional and may be omitted if they obscure timing cues.

Confirm separately:

- Whether the Level 3 scene should follow Level 2 in Build Settings or remain a workbench scene.
- Whether the proposed 340m length feels appropriate after a first graybox run.
- Whether cannon bolts should use the current homing behaviour unchanged or receive a Level 3-only steering reduction after playtesting.
- Whether the final section should keep all four trap families or remove one family if the warning load is too high at 960 x 600.

## 17. Acceptance record template

Do not mark this plan implemented until the following record is filled with measured values rather than targets:

| Check | Result | Evidence / file |
|---|---|---|
| Spike outbound travel time | TBD | Timing test or profiler capture |
| Spike return travel time | TBD | Timing test or profiler capture |
| Spike cycle duration | TBD | Timing test or profiler capture |
| Maximum logical crowd loss per pulse | TBD | Playtest log |
| Maximum logical crowd loss per shuttle leg | TBD | Playtest log |
| Maximum active projectiles | TBD | Profiler/editor observation |
| Small-crowd frame time / GC | TBD | Profiler capture |
| Normal-crowd frame time / GC | TBD | Profiler capture |
| Large-crowd frame time / GC | TBD | Profiler capture |
| 960 x 600 HUD check | TBD | Screenshot |
| Narrow/mobile-like HUD check | TBD | Screenshot |
| Chromium WebGL pass | TBD | Build/browser test note |
| Lower-powered browser pass | TBD | Build/browser test note |
| Build Settings scene order | TBD | `ProjectSettings/EditorBuildSettings.asset` |

