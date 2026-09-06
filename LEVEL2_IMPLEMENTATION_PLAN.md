# Level 2 Implementation Plan — Spiral Trapworks

This document is the approved design reference for constructing Level 2. It is a planning document only; scene, script, prefab, and ProjectSettings changes must be implemented separately and verified against this plan.

## 1. Design goals

- Make Level 2 approximately 35–40% longer than Level 1.
- Increase difficulty through trap combinations and timing, not only through object count.
- Teach each trap individually before combining it.
- Preserve at least two readable escape lanes through the first 70% of the level.
- Reuse the current ConeHazard, CircularSaw, FallingHazard, and Door behavior unless a measured design problem requires a system change.
- Remain WebGL-friendly by keeping trap counts bounded and avoiding unnecessary runtime spawning.

## 2. Coordinate convention

Use the same coordinate convention as Level 1:

- Track center: x = 0
- Track surface: y = 0
- Forward direction: positive z
- Track width: 10m
- Recommended lane centers: x = -3, -1, 1, 3
- Approximate track boundary: x = -5 to x = 5
- Level 2 length: 300m
- Track position: (0, -0.1, 150)
- Player start: approximately (0, 0, 3)
- Finish area: z = 292–298

All coordinates below are world coordinates when the Level 2 root is at (0, 0, 0). If the root is moved, apply the root offset to every position.

Avoid placing lethal hazards closer than 1.2m to the outer boundary. The current crowd system also removes runners near the edge.

## 3. Level flow

| Section | Z range | Purpose |
|---|---:|---|
| Start runway | 0–18 | Crowd control and camera introduction |
| Gate A | 24 | First strategic decision |
| Cone Weave | 38–56 | Basic lateral movement |
| Falling Trap Tutorial | 70–88 | Introduce falling hazards |
| Gate B | 104 | Stronger multiplier decision |
| Gate + Cone Combination | 120–136 | Decision immediately followed by movement |
| Saw Relay | 140–190 | Saws combined with falling hazards |
| Crossfire Section | 200–230 | Multiple trap types with reduced recovery |
| Gate C | 240 | Final economy/risk decision |
| Final Gauntlet | 252–290 | Highest difficulty |
| Finish buffer | 292–300 | Recovery and finish presentation |

Level 1 currently uses a 10m × 220m track, early gates around z=15 and z=35, cones around z=45–55, and saws around z=62–72. Level 2 keeps the same readable progression while extending the run.

## 4. Shared track coordinates

| Object | Position / size |
|---|---|
| Track | Position (0, -0.1, 150); size 10 × 0.2 × 300 |
| Start line | (0, 0.01, 3) |
| Player start | (0, 0, 3) |
| Finish marker | (0, 0.02, 294) |
| Finish gate | (0, 0, 297) |
| Camera look-ahead | Approximately 8–12m |

Keep the track straight for this level. Difficulty should come from timing, lane choices, and crowd management rather than curved or segmented geometry before the crowd boundary system supports it.

## 5. Gate plan

### Gate A — early decision

Position: Gate A root at (0, 0, 24)

Values:

Left: +25  
Right: x2

Make the gate readable from approximately z=18. Leave at least 7m of clear track after it. Do not place a lethal hazard before z=34.

### Gate B — stronger multiplier

Position: Gate B root at (0, 0, 104)

Values:

Left: +40  
Right: x2.5

Make the gate readable from approximately z=96. Avoid x3 or higher until logical crowd counts and WebGL performance are measured.

### Gate C — final decision

Position: Gate C root at (0, 0, 240)

Values:

Left: +60  
Right: x2.5

This is the final economy decision before the gauntlet. Leave at least 10m of open track after the gate so the player can read the final trap pattern.

The gate itself must remain unobstructed. The current Door behavior triggers once and determines the selected side from the player’s relative X position, so hazards must not overlap the trigger zone.

## 6. Coordinate sheet

### Cone Weave

Use two cones per row so that at least two lanes remain open.

| Z | Cone X positions |
|---:|---|
| 38 | -3.2, 3.2 |
| 42 | -1.2, 3.2 |
| 46 | -3.2, 1.2 |
| 50 | -1.2, 3.2 |
| 54 | -3.2, 1.2 |

Suggested settings:

killRadius approximately 0.85  
boundaryX = 5

Purpose: establish a left-right weaving rhythm without creating a full-width lethal wall.

### Falling Trap Tutorial

| Object | Position |
|---|---|
| Falling Trap 1 | (-2.4, 0, 70) |
| Falling Trap 2 | (2.4, 0, 88) |

Suggested settings:

floatingHeight = 5  
triggerRadius = 7–7.5  
killRadius = 1.2  
resetDelay = 1.5

The current falling hazard uses an approximately 8m trigger distance. Keep falling hazards at least 16m apart unless the trigger radius is reduced. Provide a visible warning, shadow, or ground marker.

### Gate + Cone Combination

| Z | Cone X positions |
|---:|---|
| 120 | 0 |
| 125 | -3.3, 3.3 |
| 130 | -1.4, 1.4 |
| 135 | -3.3, 3.3 |

The center cone requires an immediate lane decision. The alternating rows prevent simply holding one direction. Keep approximately 15m of readable approach after Gate B.

### Saw Relay

| Object | Position |
|---|---|
| Entry cones | (-3.2, 0, 140), (3.2, 0, 140) |
| Saw 1 | (0, 0, 148) |
| Falling Trap | (2.4, 0, 166) |
| Saw 2 | (0, 0, 184) |

Suggested saw settings:

useExplicitXLimits = true  
leftLimitOverride = -3.8  
rightLimitOverride = 3.8  
moveSpeed = 3–4  
killRadius approximately 0.8

Sequence purpose:

1. Cones force a lateral change.
2. Saw 1 tests timing after the lane change.
3. The falling trap tests warning recognition.
4. Saw 2 prevents an immediate return to the center.

Do not place two saws at the same Z coordinate. Keep at least 12m between moving hazards.

### Crossfire Section

| Z | Hazard |
|---:|---|
| 200 | Cone pair at -3, 3 |
| 206 | Cone pair at -1.3, 1.3 |
| 214 | Falling Trap at x=0 |
| 230 | Saw centered on the track |

This is the first section that should require planning several moves ahead. Keep two lanes technically open, do not hide the falling trap behind the cone rows, and leave at least 12m between the falling trap and the saw.

### Final Gauntlet

| Z | Hazard |
|---:|---|
| 252 | Cone pair at -3.2, 3.2 |
| 258 | Cone pair at -1.3, 1.3 |
| 266 | Saw 1 |
| 278 | Falling Trap at x=2.2 |
| 290 | Saw 2 |
| 294 | Recovery / finish approach |
| 297 | Finish gate |

The last 4–6m must remain clear for the finish trigger and celebration.

## 7. Difficulty progression

Increase difficulty in this order:

1. Reduce recovery space between hazards.
2. Alternate hazard positions.
3. Combine static and moving traps.
4. Place a gate before a trap pattern.
5. Increase saw movement speed slightly.
6. Reduce the number of safe lanes only near the end.

Do not increase every variable at once. The player should understand why runners were lost.

| Section | Safe lanes | Hazard count | Difficulty |
|---|---:|---:|---|
| Cone Weave | 2–3 | 10 cones | Medium |
| Falling Tutorial | 2–3 | 2 falling traps | Medium |
| Gate + Cones | 2 | 7 cones | Medium-high |
| Saw Relay | 2 | 2 saws + 1 falling trap | High |
| Crossfire | 2 | 6 cones + 1 falling trap + 1 saw | High |
| Final Gauntlet | 1–2 | 4 cones + 2 saws + 1 falling trap | Very high |

## 8. Scene construction plan

When implementation is approved:

1. Duplicate Level1.unity to Assets/Scenes/Level2.unity.
2. Preserve the Player, camera, EventSystem, HUD, wallet, and finish UI wiring.
3. Rename the main root to Level 2 - Spiral Trapworks.
4. Replace the Level 1 track with the 10m × 300m track.
5. Remove the Level 1 gate and saw groups.
6. Create these organized parents:

    Systems

    Track

    Gates

    Trap Sections/Cone Weave

    Trap Sections/Falling Tutorial

    Trap Sections/Gate Combination

    Trap Sections/Saw Relay

    Trap Sections/Crossfire

    Trap Sections/Final Gauntlet

    Pickups

    Lighting

    FinishGate

7. Reuse existing trap prefabs and scripts.
8. Add only the required trap instances.
9. Update the HUD title and world-level label.
10. Add Level 2 to Build Settings only after the scene is playable.

Do not create duplicate global UI or wallet authorities. Keep the existing LevelHud/UIManager compatibility decision documented in the root AGENTS.md.

## 9. WebGL and runtime constraints

- Keep trap objects bounded and scene-authored.
- Avoid gameplay-time Instantiate/Destroy for recurring hazards.
- Cache crowd-manager references; do not add repeated searches inside hot paths.
- Avoid adding physical track narrowing until segmented boundary detection is supported.
- Keep no more than two moving hazards visibly active in the same encounter window.
- Use explicit saw limits for deterministic behavior.
- Keep falling traps on a flat track so their ground raycast remains reliable.
- Verify visual clone counts, allocations, and frame time with small, medium, and compressed-large crowds.

## 10. Playtesting gates

Before accepting Level 2:

- Each trap is visible at least 6–10m before contact.
- Every section has a reliable survival route.
- No trap kills the entire crowd in one frame unintentionally.
- Falling traps do not activate in overlapping chains.
- Saw movement is readable and deterministic.
- Logical crowd counts do not create excessive visual clones.
- No recurring gameplay GC spikes are introduced.
- Both left and right gate choices can complete the level.
- The level completes in WebGL with the existing memory, compression, and thread settings.
- Test at the current 960×600 web reference and a narrow/mobile-like aspect ratio.
- Test in a Chromium-based browser and a lower-powered browser profile.

## 11. Review decisions

Before implementation begins, confirm:

- 300m track length.
- Three gates at z=24, z=104, and z=240.
- Whether the proposed gate values are acceptable.
- Whether the final gauntlet should use all three trap types.
- Whether pickups should be placed in the recovery spaces.
- Whether Level 2 replaces or follows SampleScene/Level1 in Build Settings.

## 12. Requested Level 2 follow-up features

This section plans three post-construction improvements for Level 2. The proposed display name is **Level 2 - Momentum Trapworks**. It follows the style of Level 1's “Multiplier Ramp-Up” name and communicates the two new gameplay ideas: increasing movement momentum and hidden falling traps.

Implementation status: the requested UI/name update, hidden one-shot falling hazards, and Level 2 distance-speed configuration are now implemented. The acceptance checks and remaining build-flow/manual-route checks below still apply.

### 12.1 UI and level naming

#### Player-facing copy

Use these exact values unless the name is changed during review:

| Surface | Value | Owner / location |
|---|---|---|
| Main HUD level label | `LEVEL 2` | `LevelHud.levelText` driven by `LevelHud.levelLabel` |
| Main HUD subtitle or combined label | `MOMENTUM TRAPWORKS` or `LEVEL 2 - MOMENTUM TRAPWORKS` | Existing LevelHud title area; prefer one combined line if no subtitle field exists |
| World-space level label | `LEVEL 2 - MOMENTUM TRAPWORKS` | Level 2 scene's world-level label under `Player` |
| Finish / win presentation | `LEVEL 2 COMPLETE` with `MOMENTUM TRAPWORKS` as the supporting name | Existing finish overlay flow |
| Scene root | `Level 2 - Momentum Trapworks` | `Assets/Scenes/Level2.unity` hierarchy |
| Camera / Cinemachine names | `Level 2 Camera`, `CM Level2 Crowd Follow` | Level 2 scene object names only |

#### Implementation boundary

1. Keep `Level1.unity` text unchanged. The requested copy is a Level 2 scene override, not a global replacement.
2. Set the Level 2 `LevelHud.levelLabel` serialized value to `LEVEL 2 - MOMENTUM TRAPWORKS` unless the HUD is split into title and subtitle fields.
3. Update any static TextMeshPro object in Level 2 that still says `Level 1`, `LEVEL 1`, or `Multiplier Ramp-Up`.
4. Check the dynamically generated finish overlay and the legacy `UIManager` path. Update only the path that is active in Level 2; retain compatibility code for Level 1 and SampleScene.
5. Do not add another UI controller. `LevelHud` remains the Level 2 HUD owner, while `UIManager` remains compatibility-only until a deliberate migration.
6. Test at 960 x 600 and a narrow/mobile-like aspect ratio so the longer name does not clip, overlap the count badge, or push the progress bar out of view.

#### UI acceptance criteria

- No visible Level 1 label appears anywhere during a Level 2 run, pause screen, restart, or finish screen.
- The Level 2 name is readable before movement begins and remains readable after the HUD refreshes.
- Level 1 still displays its original name when opened separately.
- The name update does not change HUD ownership, pause behavior, currency, progress, or scene-loading behavior.

### 12.2 Hidden falling hazards with trigger reveal

#### Desired player experience

Each falling trap is completely invisible while the player approaches. When the player crosses its trigger line, the trap becomes visible above the track and immediately begins its gravity drop. The reveal should be surprising but still give enough reaction time for a skilled player to move laterally.

The player should not see a hovering mesh, renderer shadow, warning marker, or idle bob before activation. Editor gizmos may remain visible for level-authoring and debugging only.

#### Trigger coordinates

Use a forward trigger line for each existing Level 2 falling trap. The line spans the full 10m track width so activation depends on forward progress, not which lane the player occupies.

| Hazard | Hazard position | Trigger line | Reveal-to-impact window |
|---|---:|---:|---:|
| Falling Trap 1 | (-2.4, 0, 70) | z = 63 | approximately 0.6-0.8s |
| Falling Trap 2 | (2.4, 0, 88) | z = 81 | approximately 0.6-0.8s |
| Falling Trap 3 | (2.4, 0, 166) | z = 159 | approximately 0.6-0.8s |
| Falling Trap 4 | (0, 0, 214) | z = 207 | approximately 0.6-0.8s |
| Falling Trap 5 | (2.2, 0, 278) | z = 271 | approximately 0.6-0.8s |

The 7m offset is the starting value. Re-tune it after the speed ramp is active so the trap lands near its authored Z position at both the beginning and end of the run. If the player can cross more than 7m during the drop, move the trigger line farther back rather than increasing gravity enough to make the fall unreadable.

#### Recommended state flow

Refactor `FallingHazard` into an explicit one-shot-per-run sequence:

```text
Hidden / Armed
    player crosses activation Z
        enable visual renderers
        reset to floating position
        enter Falling
Falling
    apply gravity and perform one distance-based runner hit
    when the object reaches ground: enter Landed
Landed
    remain on the track for the reset delay, then disable renderers
    enter Spent for the rest of this run
```

The current Level 2 trap should not repeatedly reset behind the player. A scene restart resets all traps to `Hidden / Armed`, which is sufficient for a forward-only level.

#### Script and scene changes

1. In `FallingHazard.cs`, add an armed/hidden state or equivalent `hasTriggeredThisRun` guard.
2. Disable every visual `Renderer` in `Awake` or `Start`, including child renderers and any shadow-producing renderer. Do not disable the script or the trigger logic.
3. Replace the current 3D distance check with a cached forward-progress check, for example `playerZ >= hazardZ - activationOffset`. A horizontal X check must not prevent a player in another lane from activating the trap.
4. Keep the existing no-physics-collider runner kill test unless a measured collision problem appears. The hidden hazard still needs no physical body; only its activation sensor is logical or trigger-only.
5. On activation, enable renderers before setting the state to `Falling`, place the object at `groundY + floatingHeight`, clear its vertical velocity, and begin the drop in the same frame.
6. Keep `floatingHeight = 5`, `gravity = -20`, and `killRadius = 1.2` as initial values. With a 5m drop and -20m/s2 gravity, the unmodified fall takes roughly 0.7 seconds.
7. If a physical trigger volume is preferred during implementation, use a child `Trigger` object with a `BoxCollider` set to `isTrigger = true`, center `(0, 1, -7)`, and size `(9.5, 2, 1.5)` in the hazard's local space. Filter activation to the Player's `CharacterController`; do not react to every visual crowd runner.
8. Prefer the logical Z check first because it avoids extra trigger traffic from the pooled visual crowd and is deterministic on WebGL. Use the physical trigger only if the logical check cannot match the desired lane-independent timing.

#### Hidden-hazard acceptance criteria

- At z = 55, 73, 151, 199, and 263 respectively, each trap's visible renderers are disabled while the player has not crossed its trigger line.
- Crossing z = 63, 81, 159, 207, or 271 reveals only the corresponding trap and starts its fall.
- No trap is visible or falling before its own trigger line.
- Each trap activates once per run, does not create allocations every frame, and is reset by restarting the scene.
- A route that changes lanes in reaction to the reveal can survive each section; no single trap unintentionally wipes the full crowd.
- The surprise effect does not hide the finish trigger or make the trap impossible to understand after the first attempt.
- WebGL playtest shows no recurring GC spikes or trigger-event flood from visual crowd members.

### 12.3 Distance-based forward-speed ramp

#### Proposed Level 2 speed profile

Start at the current 6m/s and increase smoothly to 10m/s by the finish. The target is a continuous distance-based value, not a sudden speed change at trap boundaries.

| Player Z | Normalized run progress | Target forward speed |
|---:|---:|---:|
| 3 | 0% | 6.0m/s |
| 70 | approximately 23% | 6.9m/s |
| 140 | approximately 47% | 7.9m/s |
| 210 | approximately 71% | 8.8m/s |
| 280 | approximately 94% | 9.8m/s |
| 297 | 100% | 10.0m/s |

Initial formula:

```text
progress = clamp01((playerZ - 3) / (297 - 3))
normalSpeed = lerp(6.0, 10.0, progress)
```

Use a small smoothing step only if direct interpolation is visibly abrupt. Do not add random speed changes, speed boosts from individual hazards, or a speed increase during combat.

#### Script boundary

1. In `PlayerController.cs`, replace the constant-speed assumption with serialized Level 2 tuning fields: `startForwardSpeed = 6`, `maxForwardSpeed = 10`, `speedRampStartZ = 3`, and `speedRampEndZ = 297`.
2. Compute the current speed once per movement update and use it for both normal movement and combat movement. Combat continues to use the existing 0.25 factor applied to the current speed.
3. Change the public `ForwardSpeed` property to expose the current computed speed, because `PlayerCrowdManager.cs` uses it when moving falling runners. This keeps the player and detached runners synchronized.
4. Preserve the current horizontal speed of 8m/s initially. Reassess it only if the final 20% becomes impossible to steer; horizontal control should not silently scale with forward speed during this pass.
5. Configure the speed values on the Level 2 Player instance or scene override. Do not change Level 1's baseline speed unless a separate Level 1 balance decision is approved.
6. Keep all speed calculations allocation-free and based on cached component references. Do not search for the player or scene objects inside the per-frame speed calculation.

#### Speed acceptance criteria

- A one-second measurement at the checkpoints above stays within +/-0.2m/s of the target outside combat.
- The player reaches the finish faster than with the constant 6m/s profile, while still having a readable reaction window for the final gauntlet.
- Combat applies the same relative slowdown at every distance; it does not reset the ramp or jump to the starting speed.
- Falling runners moved by `PlayerCrowdManager` use the current speed rather than the original 6m/s constant.
- Level 1 remains unchanged when tested in its own scene.
- No movement jitter, tunneling through hazards, camera lag, or frame-time spike appears at the 10m/s cap.

### 12.4 Implementation order and verification

Implement the follow-up in this order:

1. Rename the Level 2 root and update all Level 2 UI/world/finish labels.
2. Add the distance-based speed calculation and verify player/crowd synchronization.
3. Convert the falling hazards to hidden, forward-triggered, one-shot traps.
4. Re-tune each trigger line after speed is active, preserving the table coordinates unless playtesting proves the reveal window is too short.
5. Run left-lane, right-lane, and mixed-lane route tests through every trap section.
6. Run a no-input smoke test only as a negative test; it is expected to fail at the central Gate Combination cone and must not be used as proof that every route is impossible.
7. Validate the Level 2 HUD at 960 x 600 and narrow/mobile-like aspect ratios.
8. Profile a small crowd, a normal crowd, and a compressed-large crowd in WebGL. Record frame time, allocations, active renderers, and hazard activation count.
9. Only after the scene passes these checks, revisit the unresolved Build Settings order and the separate x2.5 gate-multiplier decision.

### 12.5 Current implementation record

- UI/name: implemented in Level2. The root is `Level 2 - Momentum Trapworks`; the HUD displays `LEVEL 2  •  MOMENTUM TRAPWORKS`; the HUD subtitle is `MOMENTUM TRAPWORKS`; the world label is `LEVEL 2`; and the Level 2 finish overlay adds `LEVEL 2 COMPLETE` / `MOMENTUM TRAPWORKS`.
- Falling hazards: implemented in `FallingHazard.cs`. Level 2 traps start with renderers disabled, activate from forward Z lines 7m before z=70/88/166/214/278, fall immediately, and become spent after their first drop.
- Speed ramp: implemented in `PlayerController.cs` and configured on the Level 2 Player instance. The measured profile is 6.00m/s at z=3, 6.91 at z=70, 7.86 at z=140, 8.82 at z=210, 9.77 at z=280, and 10.00 at z=297. Level 1 keeps its 6m/s default because its maximum speed remains 6m/s.
- Runtime verification: Level2 reloads cleanly, starts with all five falling hazards hidden, reveals the first trap only after its z=63 trigger line, and produced no source-code compilation errors during the test run.
- Still pending: steering route tests through every section, a real WebGL build/browser pass, and the approved Build Settings order. The editor Play test can validate scene behavior while Level2 remains at buildIndex `-1`.
