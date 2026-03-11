# Basic Project Setup (EveriTechTest)

This setup guide is for running the current slot-machine prototype and framework scene in Unity.

## 1) Open the project

1. Open Unity Hub.
2. Add/open this project folder.
3. Use the project’s configured Unity version (from `ProjectSettings/ProjectVersion.txt`).

## 2) Open the integration scene

Use the current framework integration scene:

- `Assets/Scenes/SlotFrameworkBootstrap.unity`

This scene keeps the prototype visuals/animations while using the modular math + engine flow.

## 3) Verify required scene references

Select the object with `MachineController` and confirm these are assigned:

- `Reels` root transform with reel children.
- `SlotMathConfig` (required for production mode; dev mode can fallback).
- `RuntimeModeConfig` (optional, controls strict production behavior).
- `BonusTracker`, `FlyUp`, `MeterValue`, and `SymbolRegistry` references (auto-cached if missing).

## 4) Configure math source

Open the assigned `SlotMathConfig` asset and verify:

- `Xlsx Relative Path` points to the workbook under `Assets/StreamingAssets` (default: `Math/SlotMathTemplate.xlsx`).
- Optional runtime JSON asset (`SlotMathRuntimeAsset`) if you want to avoid loading XLSX at runtime.
- `Prefer Runtime Asset` enabled only when the runtime asset is generated and up-to-date.

## 5) Configure runtime mode

Open `RuntimeModeConfig`:

- **Production mode ON**: missing or invalid math config/load will throw and stop startup.
- **Production mode OFF**: math loading errors fallback to `DefaultSlotMathModel`.
- `AllowLegacyFallbackInDevelopment` only affects development and legacy stop-symbol fallback behavior.

## 6) Play + interaction

1. Enter Play Mode.
2. Trigger spins through the existing UI button flow (`MachineController.SpinCheck`).
3. Observe economy loop:
   - Bet deducted at spin start.
   - Payout credited at settlement.
   - Spin rejected if insufficient funds.
4. Optional: use debug seed UI (if enabled in inspector) to apply deterministic RNG for reproducible spins.

## 7) Optional editor workflows

From `SlotMathConfig` inspector buttons:

- **Validate Math**: schema + referential integrity checks.
- **Generate Runtime Asset**: serializes workbook math into `SlotMathRuntimeAsset`.
- **Run Simulation**: runs seeded simulations and optionally exports CSV reports.

## 8) Common troubleshooting

- **Workbook not found**: verify `SlotMathConfig.ResolveXlsxPath()` resolves to an existing file under `Assets/StreamingAssets`.
- **Scene spins but symbols look wrong**: rebuild `SymbolRegistry`/reel caches by checking `ImageSetter.Symbols` prefab lists and `PrefabKey` mappings in the workbook.
- **No bonus trigger presentation**: ensure a symbol is flagged `IsBonus`, bonus appears on reels, and `BonusTracker`/`FlyUp` refs are present.
