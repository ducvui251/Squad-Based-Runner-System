# SpiralSquad Agent Instructions

## Project Identity

SpiralSquad is a Unity 6 crowd-runner inspired by Crowd Master-style mobile games: steer a growing squad forward, choose math gates, fight enemy mobs, avoid hazards, and finish with as many runners as possible. Keep the project distinct by emphasizing its own signature mechanic: a Fermat/golden-angle spiral crowd formation.

Do not copy protected Crowd Master assets, names, levels, UI, or exact presentation. Use it only as genre reference for readable mobile-runner pacing, clear gates, obvious crowd counts, short feedback loops, and satisfying mob encounters.

## Current Project Facts

- Unity project path: `C:\Users\Me\Documents\UnityProjects\SpiralSquad`
- Main scene: `Assets/Scenes/SampleScene.unity`
- Unity version from README: `6000.3.16f1`
- Target feel: mobile-first 3D crowd runner with simple steering and immediate visual feedback.
- MCP endpoint: `http://localhost:26560`
- Workspace MCP file: `.mcp.json`

## Collaboration Rules

- Prefer making concrete progress over asking command-approval questions. Ask the user about gameplay, scene layout, art direction, balancing, destructive edits, or ambiguous design intent.
- Before scene edits, inspect relevant GameObjects/components first when practical.
- Preserve user changes. The worktree may be dirty; do not revert unrelated files.
- Do not delete user-created assets or scene objects unless the user explicitly asks or the object is clearly temporary and created by the current task.
- Save scenes only when asked, or after the user has clearly requested direct scene edits that should persist.
- Keep answers concise and report the actual files/scenes touched plus verification performed.

## Unity MCP Workflow

- Use Unity MCP tools for direct scene and prefab manipulation whenever available.
- Use `scene-list-opened`, `scene-get-data`, `gameobject-find`, `gameobject-component-get`, and `object-get-data` to inspect before modifying unknown objects.
- Use `gameobject-modify`, `gameobject-component-modify`, `gameobject-create`, `gameobject-set-parent`, prefab tools, and ProBuilder tools for scene edits.
- Use `assets-refresh` after external file changes that Unity needs to import.
- Use screenshots (`screenshot-game-view`, `screenshot-scene-view`, or isolated screenshots) when visual placement or framing matters and the tools are enabled.
- If MCP is unreachable, check `unity-mcp-cli status`, `.mcp.json`, and whether Unity is running before changing project files.

## Gameplay Direction

- Treat Crowd Master as genre inspiration: clear gate choices, count badges, enemy groups, crowd loss, and readable forward-runner lanes.
- SpiralSquad's differentiator is the sunflower/Fermat spiral formation. Preserve and improve this identity rather than replacing it with a plain grid or blob crowd.
- Favor short, readable encounters: gates before hazards, hazards before enemies, recovery opportunities after losses, and visible risk/reward choices.
- Math gates should be instantly legible (`+N`, `xN`) and positioned as meaningful left/right lane decisions.
- Enemies should read as mobs with group alert behavior. Encounters should cost runners but avoid sudden unfair total wipes.
- Hazards should be telegraphed, physically readable, and tuned around the crowd width and edge-fall logic.

## Existing Architecture

- `Assets/Scripts/PlayerController.cs`: CharacterController movement, drag/keyboard steering, jump, gravity.
- `Assets/Scripts/PlayerCrowdManager.cs`: logical runner count, visual clone pool, Fermat spiral layout, edge-falling, combat, game-over checks.
- `Assets/Scripts/Gate.cs` and `Assets/Scripts/Door.cs`: add/multiply crowd math and gate text.
- `Assets/Scripts/Enemy.cs`: enemy detection, group alert, chasing, one-vs-one combat.
- `Assets/Scripts/EnemySpawner.cs` and `Assets/Scripts/GroupSpawner.cs`: enemy group placement.
- `Assets/Scripts/CircularSaw.cs`, `FallingHazard.cs`, `ProjectileLauncher.cs`, `TrackingProjectile.cs`: hazards.
- `Assets/Scripts/UIManager.cs`: menu/HUD/game-over/win flow and Cinemachine fall camera.
- `Assets/Scripts/CountBadge.cs`: world-space count badges for player and enemy groups.

## Code Constraints

- Keep runtime hot paths allocation-conscious. Existing systems use pools, `OverlapSphereNonAlloc`, `RaycastNonAlloc`, cached arrays, and logical-to-visual crowd compression.
- Avoid gameplay-time `Instantiate`/`Destroy` in crowd systems unless creating one-shot effects or explicitly replacing the architecture.
- Clamp serialized counts and sizes with `OnValidate` when exposing values that can create huge scenes or frame spikes.
- Use serialized fields for designer-tunable values. Keep public API small and intentional.
- Preserve mobile/WebGL performance: simple shaders/materials, limited active clone counts, clear physics layers/tags, and no expensive per-runner collision solving.
- Prefer local fixes consistent with existing scripts over broad refactors.

## Scene And Level Editing Rules

- Maintain a forward-running Z-axis track with clear left/right steering space.
- Keep player start, camera, gates, enemies, hazards, and finish markers aligned to readable lanes.
- When placing objects, use descriptive names and group related objects under parent folders such as `Gates`, `EnemyGroups`, `Hazards`, `Track`, or `Finish` when that matches the scene.
- Avoid overlapping UI, gates, hazards, and enemy groups in ways that obscure player decisions.
- Gate/door colliders should be triggers and should not block movement unless intentionally designed as physical obstacles.
- Enemy and hazard placement should respect the current player speed, crowd width, and visible reaction time.

## Visual Direction

- Favor high-contrast, readable mobile visuals over detailed clutter.
- Player crowd should remain visually distinct from enemies and hazards.
- Count badges should be visible but not block the action.
- Avoid one-note palettes; use color to communicate function: player, enemies, positive gates, dangerous hazards, finish/reward.

## Verification

- For code changes, run the narrowest useful checks available. If Unity tests are relevant and enabled, use `tests-run`.
- For scene edits, verify via scene/game screenshots when possible.
- For gameplay changes, prefer Play Mode validation when practical; otherwise inspect serialized values and relevant components.
- Report any checks that could not be run and why.
