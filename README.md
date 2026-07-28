# Dangaronpo

A 3D first-person mystery escape room prototype made with Unity 2022.3.62f3c1.

This is a fan-made portfolio prototype inspired by high-stakes escape-room mystery games. The project focuses on a complete playable puzzle flow rather than large-scale production content.

## Features

- First-person movement and center-screen raycast interaction
- Investigation prompt and text/image clue UI
- Inventory system with item selection, take-out, put-away, and item combination
- ScriptableObject-driven item data
- Drawer, safe, laptop password, four-light sequence, and final revolver puzzle flow
- Pickup confirmation UI for newly acquired key items
- BGM/SFX playback and volume settings UI
- Main menu, settings panel, and ending UI

## Controls

- `WASD`: Move
- Mouse: Look
- `E`: Interact / confirm
- `Tab`: Inventory
- `Esc`: Close UI / exit current UI mode
- `F1`: Settings in gameplay scene

## Project Structure

Main project-owned files are under:

```text
Assets/_Project/
  Art/
  Audio/
  Materials/
  Prefabs/
  Scenes/
  ScriptableObjects/
  Scripts/
```

Imported third-party models may remain under `Assets/Model/` or `Assets/Prefab/`.

## How To Open

1. Install Git LFS.
2. Clone the repository.
3. Run `git lfs pull` after cloning.
4. Open the project with Unity Hub using Unity `2022.3.62f3c1`.
5. Open `Assets/_Project/Scenes/MainMenu.unity` or `Assets/_Project/Scenes/SampleScene.unity`.

## Notes

This repository is intended as a learning and portfolio prototype. Third-party assets, fonts, sounds, and references should be reviewed before any public release or commercial use.
