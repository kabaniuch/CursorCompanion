# Cursor Companion 2.5D

A desktop pet application (Tiger) for Windows. The pet lives in a transparent, borderless, always-on-top window. It falls onto other windows and the taskbar, responds to right-click drag, plays sprite animations, supports hotkey actions with cooldowns, has a system tray menu, and MVP direct-IP multiplayer.

## Requirements

- .NET 8 SDK (or .NET 9 SDK with net8.0 targeting support)
- Windows 10/11

## Build

```bash
cd D:/CursorCompanion/Projekt
dotnet build
```

## Run

```bash
dotnet run --project src/CursorCompanion.App
```

## Controls

- **Right-click drag**: Pick up and move the pet
- **Keys 1-5**: Trigger actions (Scratch, Roar, Paw Wave, Shake, Sit Pose)
- **System tray icon**: Right-click for menu (action packs, multiplayer toggle, exit)

## Project Structure

| Project | Description |
|---------|-------------|
| `CursorCompanion.App` | WPF entry point, MainWindow, tray icon, config |
| `CursorCompanion.Core` | GameLoop (60Hz), Time, StateMachine, Logger |
| `CursorCompanion.Windowing` | Win32 P/Invoke, LayeredWindow, WindowTracker, TaskbarService |
| `CursorCompanion.Rendering` | SkiaSharp renderer, SpriteAtlas, AnimationPlayer, HitMask |
| `CursorCompanion.Pet` | PetController, PetPhysics, PetInput, state machine |
| `CursorCompanion.Actions` | ActionPackService, ActionDefinition, cooldowns |
| `CursorCompanion.Networking` | LiteNetLib UDP transport, direct-IP multiplayer |

## Assets

Place atlas files in the `assets/` folder:
- `atlas.png` — sprite atlas image
- `atlas.json` — frame metadata (clips, frames, fps, loop, pivot)

If no atlas is found, a placeholder with colored rectangles is generated at runtime.

## Configuration

Edit `assets/config.json` to customize physics, cooldowns, network port, etc.

## Multiplayer

Enable multiplayer from the system tray menu. The host listens on port 7777 (configurable). A client connects by entering the host's IP address. Remote pets appear as a second sprite in the same window.
