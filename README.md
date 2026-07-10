# moyougae

`moyougae` is a Unity MR prototype for placing virtual furniture in a real room on Meta Quest.

The app uses room boundary input and controller ray interactions to build a simple room shape, then lets users place, select, move, and scale furniture models while viewing the layout in mixed reality.

## Features

- Meta Quest / Meta XR based mixed reality scene setup
- Room boundary point selection with controller ray input
- Simple room mesh generation for floors, walls, doors, and windows
- Furniture catalog placement and selection
- Move and scale controls for placed furniture
- GLB model loading and registration
- Runtime handling for generated models
- URP material adjustment for imported models

## Tech Stack

- Unity 2022.3.37f1
- C#
- Universal Render Pipeline
- Meta XR SDK
- XR Interaction Toolkit
- glTFast
- OpenUPM

## Project Structure

```text
Assets/
  Scenes/              Unity scenes
  Script/              Main application scripts
  material/            Furniture and room assets
Packages/              Unity package manifest and lock file
ProjectSettings/       Unity project settings
```

## Main Scripts

- `RoomScanManager.cs` manages room boundary point selection and scan state.
- `RoomBuilder.cs` generates room geometry from selected points.
- `FurnitureManager.cs` manages the furniture catalog, placement, generated GLB registration, and persistence.
- `FurnitureContextUI.cs` manages UI controls for the selected furniture item.
- `VREditSpawnManager.cs` manages cone-based room editing and placement flows.
- `GeneratedModelImporter.cs` and `CaptureToModelFlow.cs` handle generated GLB models for in-app use.

## Requirements

- Unity 2022.3.37f1
- Android Build Support
- Meta Quest device
- Git LFS

## Setup

1. Clone the repository.
2. Pull large assets with Git LFS.

```bash
git lfs pull
```

3. Open the project with Unity 2022.3.37f1.
4. Connect a Meta Quest device and build for Android when needed.

## Notes

- Generated folders such as `Library/`, `Logs/`, `obj/`, `.vs/`, and `UserSettings/` are not tracked.
- Build outputs such as APK files are not tracked.
- Large 3D and media assets are tracked with Git LFS.
