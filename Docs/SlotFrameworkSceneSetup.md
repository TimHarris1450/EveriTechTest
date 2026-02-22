# Slot Framework Scene: Setup & Operation

This document explains how to run the new bootstrap scene and what is wired by default.

## New Scene

A new runnable scene has been added:

- `Assets/Scenes/SlotFrameworkBootstrap.unity`

It is currently created from the working prototype scene so existing visual flow and hookups are preserved while we continue migrating systems to the modular framework.

## Build Settings

The scene is included in Unity Build Settings (`File > Build Settings`) so it can be launched directly in Play Mode or included in builds.

Current scene order:
1. `Assets/Scenes/Scene.unity`
2. `Assets/Scenes/SlotFrameworkBootstrap.unity`

## How to Run

1. Open the project in Unity.
2. Open `Assets/Scenes/SlotFrameworkBootstrap.unity`.
3. Enter Play Mode.
4. Use the existing spin button/input flow in the UI to start and stop reels.

## What Is Hooked Up

The bootstrap scene keeps all existing wiring from the prototype:

- Reel spin/stop orchestration through `MachineController`.
- Symbol replacement and bonus injection flow through `ImageSetter`.
- Bonus tracking/feature animations through `BonusTracker` + `SymbolAnimController`.
- Feature presentation through `BlackHoleController`, `FlyUp`, and `MeterValue`.

## Math-Driven Migration Compatibility

This scene is intended to be the staging scene for the migration work:

- Existing visuals and animation timings remain intact.
- Data-driven math systems can be swapped in incrementally.
- Presentation scripts can continue to be reused as engine/math loading are refactored.

## Recommended Next Setup Steps

1. Add a `SlotMathConfig` asset assignment in scene-level controller(s).
2. Bind `SlotMathLoader` initialization to scene startup.
3. Route spin requests through `SlotEngine` and feed returned `SpinResult` into presentation.
4. Keep this scene as the integration target for iterative migration tests.
