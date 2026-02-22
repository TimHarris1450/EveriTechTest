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

## Runtime Configuration Requirements

`MachineController` now expects:

1. `SlotMathConfig` assigned.
2. Optional `RuntimeModeConfig` assigned for strict startup behavior.
   - In production mode, missing/invalid workbook throws and startup stops.
   - In development mode, fallback to `DefaultSlotMathModel` is allowed.

## Economy Loop

- Integer-only economy is now session-driven (long-backed balance).
- Total bet is deducted when a spin starts.
- Payout is applied when spin settles.
- Insufficient-funds blocks spin start, but does not disable the spin button (players can keep attempting/slamming).

## Migration Notes

- Engine-driven path is the default gameplay flow.
- Legacy stop-symbol fallback is dev-only and must be explicitly enabled.
- Use `SlotFrameworkBootstrap.unity` as the integration scene for this round.
