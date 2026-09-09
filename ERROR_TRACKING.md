# Error Tracking

## Level4 jumping destroys the crowd

- Error: The lead player and clones were lost at jump blocks even with a high `jumpForce`.
- Cause: `JumpBarrierHazard` removed runners without checking whether the lead had cleared the block. The shared removal method was also used by rising blocks, shutters, and sweep beams.
- Fix: Added a shared `PlayerController.HasCleared` guard in `Level4HazardBase`, plus the barrier crossing lock. A successful airborne clearance now skips runner removal.

## Clone animation missing

- Error: The lead player animated, but visual clones became static after crowd growth or gate passage.
- Cause: The lead Animator and `RunnerPrefab` used different Avatar assets. Runtime code copied the lead controller but not the Avatar.
- Fix: Assigned the lead Avatar, `Stickman_heads_sphere@RunningAvatar`, to the Level4 `RunnerPrefab` Animator.

## Level 5 forward movement stops with a live crowd

- Error: Level 5 could stop forwarding while the player still had logical runners.
- Cause: Level 5 uses `LevelHud` without a `UIManager`, so gameplay inherited stale `UIManager.IsGameActive`/game-over state; `ConeHazard` also eliminated off-edge runners at any Z coordinate.
- Fix: `LevelHud` now registers standalone level activity, `PlayerController` ignores an inconsistent terminal flag when logical runners remain, and cone edge elimination is limited to the cone's local Z window.

## Level 5 lead fall continued forward motion

- Error: After a no-jump physical-gap failure, the lead continued running forward while falling if visual runners still kept the logical count above zero.
- Cause: `PlayerController` treated every nonzero crowd as active even after `PlayerCrowdManager` entered its lead-fall terminal state.
- Fix: Added the explicit `IsLeadFallGameOver` state and stopped forward movement for that state while leaving `PlayerCrowdManager` running its pooled falling-runner cleanup. The Level 5 no-jump probe then stopped the lead and transferred the rear visual runners into the falling pool.

## Level 5 fracture crossing conflated lead success with crowd success

- Error: The first implementation either dropped visual runners before lead clearance was known or preserved every runner after a lead success, so it could not show a spatial front-to-back split.
- Cause: `CrackedSpanHazard` treated the lead crossing as the whole crowd outcome, while `PlayerCrowdManager.ProcessRoadGap` used only a current-frame Z band with no runner identity or trajectory state.
- Fix: Each physical fracture now opens a bounded session before the near edge. The manager keys participating runners by identity, latches SAFE_NEAR_SIDE/OVER_GAP/SAFE_FAR_SIDE/FAILED from actual Z and the shared `PlayerController` trajectory, and transfers only FAILED runners once through the existing pooled fall path. Lead success no longer implies full-crowd success.

## Level 5 crossing scan could mutate its runner list

- Error: Suspected index corruption when a compressed crowd loss changed `activeRunners` during a fracture scan.
- Cause: `ProcessRoadGap` iterated the mutable active list while `MakeRunnerFall` could synchronously run logical-to-visual compression cleanup.
- Fix: The scan now iterates the bounded per-fracture identity dictionary; runner removal only changes record state, so the scan collection remains structurally stable.

## GUI HUD upgrade editor script compile error

- Error: The first editor-script verification/implementation attempt failed to compile because `OpenSceneMode` was referenced from `UnityEngine.SceneManagement`.
- Cause: `OpenSceneMode` is declared in `UnityEditor.SceneManagement`.
- Fix: Corrected the namespace and reran the editor script; all five LevelHud scenes saved successfully with no subsequent implementation errors.

## HUD level label reference was unassigned

- Error: `LevelHud.levelText` was unassigned in Level1–Level5, so the serialized `levelLabel` could not update the existing `HUD Level Title` object.
- Cause: Each scene serialized `levelText: {fileID: 0}`.
- Fix: Wired each existing `HUD Level Title` TMP component into `LevelHud.levelText` through the Unity editor and saved all five scenes. Verification found zero null assignments and one assigned reference per scene.

## PlayerCrowdManager jump-wave methods missing

- Error: `PlayerCrowdManager.cs` failed with CS0103 for `ResetJumpWave` and `UpdateJumpWaveHistory`.
- Cause: The jump-wave fields and call sites remained, but both helper method definitions were absent.
- Fix: Restored a bounded, allocation-free reset and circular trajectory-history update using the existing `PlayerController` airborne state. Unity refresh then produced no PlayerCrowdManager errors; only the pre-existing `Enemy.lastPlaybackStateHash` CS0414 warning remained.

## HUD asset swap attempted during Play Mode

- Error: The first MCP HUD swap applied runtime-only assignments and then failed at `EditorSceneManager.MarkSceneDirty` with `This cannot be used during play mode`.
- Cause: Level5 was still in Play Mode, where scene persistence is unavailable.
- Fix: Stopped Play Mode, reran the swap in Edit Mode, and saved Level5 successfully. Final verification passed with `dirty=False` and no console errors.

## HUD visibility edit used the wrong EditorSceneManager namespace

- Error: The first visibility-improvement MCP script failed to compile with CS0234 because `UnityEditor.EditorSceneManager` does not exist.
- Cause: `EditorSceneManager` is declared in `UnityEditor.SceneManagement`.
- Fix: Corrected the namespace and reran the edit; Level5 saved with `HUD_VISIBLE_VERIFY_OK` and `dirty=False`.

## GUI pack particle buffer and HUD clipping

- Error: Play Mode logged `ArgumentNullException: Value cannot be null. Parameter name: particles` from `UIParticleSystem.OnPopulateMesh`; the HUD topbar rendered white and the coin value was clipped at the right edge.
- Cause: The active `ResourceBar_Coin/Icon/Fx_Star` used the GUI pack `UIParticleSystem` while its particle buffer was null during mesh population. The topbar Image was pure white, and the 220-unit resource container was smaller than the 309-unit coin row; nested prefab layout changes also needed explicit prefab-instance overrides.
- Fix: Added a reinitialization guard before `GetParticles` in `Assets/Layer Lab/GUI Pro-CasualGame/Extensions/UIParticle/UIParticleSystem.cs`; saved Level1–Level5 overrides with navy topbar/coin backgrounds, a 360-unit resource container, a 300-unit coin row, reversed icon/value/add presentation, disabled `ContentSizeFitter`, and recorded the prefab overrides.
- Verification: `assets_refresh` with `ForceSynchronousImport` succeeded. `HUD_FIX_AUDIT` and `HUD_OVERRIDE_AUDIT` reported all five scenes saved clean with valid HUD references and the intended layout values. A fresh Play Mode rerun remains pending.
