# Error Tracking

## Level4 jumping destroys the crowd

- Error: The lead player and clones were lost at jump blocks even with a high `jumpForce`.
- Cause: `JumpBarrierHazard` removed runners without checking whether the lead had cleared the block. The shared removal method was also used by rising blocks, shutters, and sweep beams.
- Fix: Added a shared `PlayerController.HasCleared` guard in `Level4HazardBase`, plus the barrier crossing lock. A successful airborne clearance now skips runner removal.

## Clone animation missing

- Error: The lead player animated, but visual clones became static after crowd growth or gate passage.
- Cause: The lead Animator and `RunnerPrefab` used different Avatar assets. Runtime code copied the lead controller but not the Avatar.
- Fix: Assigned the lead Avatar, `Stickman_heads_sphere@RunningAvatar`, to the Level4 `RunnerPrefab` Animator.
