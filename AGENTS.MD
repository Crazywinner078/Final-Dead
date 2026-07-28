# AGENTS.MD

## Scope

These instructions apply to the whole Unity project.

Project context:
- Unity version: 2022.3.62f3c1.
- Project type: 3D first-person mystery escape room prototype.
- Render pipeline: Built-in Render Pipeline unless the project is explicitly migrated.
- Primary goal: keep code small, readable, and suitable for a portfolio-quality client/gameplay prototype.

## High-Level Direction

- Build the game around first-person room exploration, center-screen raycast interaction, puzzle state, clue/item collection, and controlled presentation moments.
- Prefer a simple playable vertical slice over large generic frameworks.
- Keep systems decoupled enough that interactables, puzzles, UI, and player control can evolve independently.
- Do not add combat, weapon, enemy, or FPS-shooter systems unless explicitly requested.

## Folder Layout

Put new project-owned files under `Assets/_Project/`.

Recommended layout:

```text
Assets/_Project/
  Art/
  Audio/
  Materials/
  Prefabs/
    Player/
    Interactables/
    Puzzles/
    UI/
  Scenes/
  ScriptableObjects/
    Clues/
    Items/
    Puzzles/
  Scripts/
    Core/
    Player/
    Interaction/
    UI/
    Puzzle/
    Evidence/
    Audio/
    Camera/
    Data/
```

Existing imported/downloaded assets may remain in `Assets/Model/` and `Assets/Prefab/` unless a cleanup pass is requested. Do not reorganize third-party assets casually because Unity `.meta` references can break.

## C# Style

- Use C# with Unity `MonoBehaviour` components unless a plain C# class is clearly better.
- Use namespace `Dangaronpo` for project scripts. Use sub-namespaces only when they add clarity, for example `Dangaronpo.Player` or `Dangaronpo.Interaction`.
- One main public type per `.cs` file. The filename must match the public type.
- Use `PascalCase` for classes, structs, enums, properties, methods, and events.
- Use `camelCase` for local variables and parameters.
- Use `camelCase` for private fields. Do not prefix private fields with underscores.
- Use `[SerializeField] private` for Inspector-configured references and tuning values.
- Avoid public fields except constants or Unity-specific cases where a public API is intentional.
- Prefer early returns over deeply nested conditionals.
- Keep `Update()` short. Delegate behavior to small private methods when it grows.
- Do not use `FindObjectOfType`, `GameObject.Find`, or tag searches in gameplay paths unless there is no reasonable serialized reference alternative.
- Avoid singletons by default. Use serialized references, scene composition, or small service components first.
- Add comments only for non-obvious behavior, design constraints, or Unity-specific setup requirements.

Example field style:

```csharp
[SerializeField] private float moveSpeed = 4f;
[SerializeField] private LayerMask interactableMask;

private CharacterController characterController;
```

## Player System

Use `CharacterController` for the first-person player unless explicitly changed.

Preferred player prefab structure:

```text
Player
  CharacterController
  PlayerMotor
  PlayerLook
  PlayerInteractor
  PlayerModeController
  CameraRoot
    Main Camera
```

Responsibilities:
- `PlayerMotor`: movement, gravity, optional sprint/crouch later.
- `PlayerLook`: mouse look, horizontal body rotation, vertical camera-root rotation, vertical clamp.
- `PlayerInteractor`: center-camera raycast, focus tracking, triggering interactables.
- `PlayerModeController`: enables/disables movement, look, and interaction for modes such as free movement, reading text, inspecting an object, and pause.

Default assumptions:
- No jump in the first playable escape-room prototype unless requested.
- Mouse sensitivity must be serialized and easy to tune.
- Camera vertical rotation should be clamped, commonly around `-75` to `75` degrees.

## Interaction System

Use a center-screen raycast from the active player camera.

Default assumptions:
- Raycast distance: about `2.5f` to `3f`.
- Interactable objects should be on an `Interactable` layer.
- Plain environment props should not be on the interactable layer.

Use an interface for all interactable targets:

```csharp
public interface IInteractable
{
    string DisplayName { get; }
    string PromptText { get; }
    bool CanInteract { get; }

    void OnFocus();
    void OnUnfocus();
    void Interact(PlayerInteractor interactor);
}
```

`PlayerInteractor` should not know whether the target is a note, drawer, item, door, safe button, computer, or puzzle panel. It only finds `IInteractable`, handles focus transitions, updates prompt UI, and calls `Interact()`.

For concrete project objects, prefer a shared `InteractableBase` MonoBehaviour that implements `IInteractable` and stores the common display/prompt/permission fields. This keeps lookup simple and avoids per-frame component scans.

Common interactable types:
- `ExamineInteractable`: shows investigation text.
- `PickupInteractable`: adds an item/clue and hides or disables the world object.
- `DoorInteractable`: opens, locks, or reports locked state.
- `PuzzleButtonInteractable`: forwards button presses to a puzzle controller.
- `InspectInteractable`: enters close-up camera/inspection mode.

## UI Rules

- Use TextMeshPro for text.
- Keep UI logic separate from gameplay logic.
- `PlayerInteractor` may call a prompt UI service/component, but it should not construct UI text panels itself.
- Reading investigation text should switch player mode away from free movement so the player does not move or rotate while reading.
- UI should be functional first; stylized animation can be added after the gameplay loop works.

Suggested UI components:
- `InteractionPromptUI`: show/hide `[E] Investigate`, `[E] Pick Up`, etc.
- `ExamineTextUI`: show/close investigation text.
- `InventoryUI` or `EvidenceUI`: added later after item/clue collection exists.

## Puzzle And Game State

- Put puzzle-specific logic in dedicated controllers, for example `SafePuzzle`, `LightSequencePuzzle`, or `DrawerPuzzle`.
- Interactable buttons should call puzzle controllers rather than owning full puzzle rules.
- Keep puzzle state explicit with clear fields such as `isSolved`, `isOpen`, `hasBattery`, or `currentCode`.
- Prefer small serializable data objects or ScriptableObjects when content grows, but do not introduce data-driven architecture before it helps.
- Avoid hard-coding text, item names, and puzzle answers throughout multiple scripts. Store each value in one obvious place.

## Assets And Prefabs

- Every interactable prefab should have a clear root name, collider setup, layer setup, and assigned script references.
- Use prefab names with a clear prefix when helpful, for example `P_Key`, `P_Scissors`, `P_Safe`, `P_LightPuzzlePanel`.
- Keep imported model meshes separate from project-owned prefabs. Do not edit imported asset hierarchy directly when a prefab wrapper can hold colliders, scripts, and effects.
- For models from third-party sources, keep attribution information in project documentation or a credits file.

## Input

Use keyboard/mouse defaults for the prototype:
- `WASD`: move.
- Mouse: look.
- `E`: interact.
- `Tab`: evidence/inventory later.
- `Esc`: close UI, leave inspect mode, or pause depending on current mode.

If the Unity Input System package is introduced later, migrate deliberately and keep input bindings centralized.

## Scene And Component References

- Prefer serialized scene references over runtime search.
- Validate required references in `Awake()` or `OnValidate()` when missing references would break play mode.
- Use clear error messages with component and GameObject names.
- Do not silently ignore missing core references.

## Coroutines And Animation

- Coroutines are acceptable for small local interactions such as door opening, text pulse, light blink, or camera transition.
- Keep coroutine ownership local to the object being animated.
- For authored cinematic sequences, use Timeline/Cinemachine only when the interaction needs it.
- Do not use physics simulation for simple UI-like mechanical animations unless physical behavior is actually required.

## Performance And Physics

- Avoid per-frame allocations in core loops such as movement and interaction.
- Keep raycasts targeted with `LayerMask`.
- Use simple colliders for interactables and props.
- MeshColliders should be avoided for small gameplay props unless necessary.

## Development Order

Prioritize systems in this order:

1. First-person movement and camera look.
2. Center raycast interaction and prompt UI.
3. Investigation text UI.
4. Pickup/item or clue collection.
5. Simple door/drawer state changes.
6. Puzzle controllers such as safe code and four-light sequence.
7. Close-up inspection mode.
8. Stylized UI animation, audio feedback, and presentation polish.

## Verification

After changing gameplay code:
- Enter Play Mode and test the main room manually.
- Confirm player movement, mouse look, interaction prompt, and `E` interaction still work.
- Check the Unity Console for errors and warnings caused by missing references.
- If scripts were renamed or moved, verify prefab component references are intact.
