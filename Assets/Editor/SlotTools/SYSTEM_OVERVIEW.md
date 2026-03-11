# Slot Tools (Editor) Documentation

These editor tools support math authoring workflows without entering play mode.

## 1) SlotMathConfig Inspector Extension

### Component
- `SlotMathConfigInspector`

### What it adds
Custom inspector actions for `SlotMathConfig`:
- Browse/select workbook path.
- Reload summary of loaded math.
- Validate workbook.
- Generate runtime asset.
- Open simulation runner.

### Small components
- Summary generation:
  - Source path.
  - SHA256 of workbook.
  - symbol count, reel lengths, paytable sizes, payout mode.
- Runtime generation flow:
  - Load workbook -> create/reuse `SlotMathRuntimeAsset` -> serialize model -> mark config to prefer runtime asset.

---

## 2) Math Validation Window

### Component
- `SlotMathValidationWindow`

### Purpose
Runs schema load + referential sanity checks on currently selected config workbook.

### Small checks performed
- Workbook existence.
- Loader/schema parse success.
- Reel length >= `VisibleRows`.
- Reel symbol IDs all defined in symbols sheet.
- Each symbol has at least one paytable row.
- `BonusEligibleReelIndices` refer to existing reels.
- Bonus paytable requires at least one `IsBonus` symbol.

---

## 3) Simulation Window

### Component
- `SlotSimulationWindow`

### Purpose
Runs deterministic simulation reports from editor.

### Small components
- Inputs:
  - `SlotMathConfig`, spin count, RNG seed, export CSV toggle.
- Built-in buckets:
  - `0`, `1-9`, `10-49`, `50+`.
- Output summary:
  - spins, seed, source, RTP, hit frequency, total payout, CSV path.

### Typical workflow
1. Assign config.
2. Set spin count + seed.
3. Run simulation.
4. Inspect summary and optionally open exported CSV in `Assets/StreamingAssets/SimulationReports`.
