# Ant Colony Current Game State

## Current milestone

**Milestone 1 — Queen Movement Prototype**  
**Status:** Planning complete; implementation has not started and is awaiting approval.

## Repository assessment

- A Unity project exists at the repository root and uses Unity `6000.3.8f1`.
- The project was created from a Unity 2D URP template.
- `Assets/Scenes/SampleScene.unity` is the only build scene. Its authored content is limited to the template `Main Camera` and `Global Light 2D`.
- The new Input System is enabled and the template input actions asset contains a `Player/Move` action.
- Unity's 2D, Tilemap, Tilemap Extras, Sprite Shape, 2D Animation, URP, and Test Framework packages are already available.
- Netcode for GameObjects is not installed. The built-in Multiplayer Center entry is present, but no networking gameplay package or networking code is present.
- No C# gameplay scripts, prefabs, ant art, Tilemaps, tunnel geometry, or tests are present.
- No Bootstrap or global game manager exists. Future work should preserve an authored-content-first approach and should not introduce one unless a later requirement genuinely needs it.

## Documentation status

- `Documentation/GameDesignDocument.md` is missing. It must be supplied or created with developer approval before it can serve as the primary design reference.
- `Documentation/DevelopmentRoadmap.md` now records the approved Milestone 1 scope and proposed implementation order.
- This file records the inspected baseline; it does not claim that gameplay has been tested.

## Implemented systems

There are currently no Ant Colony gameplay systems. The repository contains only the Unity project foundation and template assets.

## Verified working features

No gameplay feature has been verified in the Unity Editor.

Repository inspection confirmed that:

- the Unity project version and package manifests are present;
- the sample scene is included in Build Settings;
- the project uses the new Input System;
- the expected 2D physics and rendering packages are declared; and
- generated Unity folders are excluded by `.gitignore`.

These are file-level checks, not Play Mode validation.

## Known issues and risks

- The GDD is absent, so design details beyond the supplied Milestone 1 brief cannot be validated.
- The test scene, queen prefab, tunnel collision geometry, surface layer, movement code, and camera-follow code do not exist yet.
- Surface transitions around corners are the main technical risk for Milestone 1. They should be developed after flat floor, wall, and ceiling movement work independently.

## Required Unity Editor setup for Milestone 1

After implementation is approved, the following authored setup will be required in the Unity Editor:

1. Create and save an Ant Colony prototype scene under `Assets/AntColony/Scenes`.
2. Create a `ClimbableSurface` physics layer.
3. Build a small continuous tunnel using simple 2D colliders initially, or a Grid and Tilemap with `TilemapCollider2D` if representative tiles are available.
4. Create a queen GameObject with:
   - a placeholder `SpriteRenderer`;
   - a `Rigidbody2D` configured for the movement prototype;
   - a simple `CapsuleCollider2D` or `CircleCollider2D` sized to the placeholder art; and
   - the future queen movement component.
5. Save the queen as a prefab under `Assets/AntColony/Prefabs/Ants`.
6. Assign the `ClimbableSurface` layer mask and other movement tuning values in the queen component's Inspector.
7. Add the future camera-follow component to the Main Camera and assign the queen as its target.
8. Add the prototype scene to Build Settings and remove or disable `SampleScene` once the replacement scene is verified.

Exact Rigidbody2D settings, physics query distances, speeds, and smoothing values should be documented after they have been tuned and verified in Unity rather than guessed in advance.

## Next approved development step

Wait for developer approval of the assessment and Milestone 1 plan. After approval, create only the minimal Milestone 1 folder structure and authored prototype scene setup, then implement and verify horizontal floor movement before adding wall or ceiling traversal.
