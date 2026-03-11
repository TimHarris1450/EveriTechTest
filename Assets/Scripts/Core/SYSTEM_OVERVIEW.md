# Core Systems Documentation

This folder contains the deterministic slot framework systems (math model, loading, spin resolution, payouts, economy/session, and simulation).

## 1) Math Domain System (`Core/Math`)

### Purpose
Defines the runtime data model used by every other core subsystem.

### Small components
- `SlotMathModel`
  - Container for `Symbols`, `Reels`, `Paytable`, `BonusPaytable`, and `Config`.
- `SymbolData`
  - Symbol identity and behavior flags (`IsWild`, `IsScatter`, `IsBonus`) and `PrefabKey` for presentation mapping.
- `ReelStrip`
  - Ordered symbol IDs by reel index.
- `PaytableEntry`
  - Symbol/count to payout mapping.
- `BonusPaytableEntry`
  - Bonus-symbol count to payout mapping.
- `SlotMathRuntimeConfig`
  - Runtime config like `VisibleRows`, `BonusEligibleReelIndices`, and `PayoutMode`.
- `PayoutMode`
  - Currently supports `SingleCenterLine` primary pay evaluation.

### How to extend safely
1. Add new payout modes to `PayoutMode`.
2. Implement payout logic in `PayoutCalculator.EvaluateConfiguredPrimaryMode`.
3. Add schema parsing support in math loading (`SlotMathLoader.ParseConfig`).

---

## 2) Math Loading System (`Core/MathLoading`)

### Purpose
Loads and validates slot math from XLSX and/or runtime assets.

### Small components
- `SlotMathConfig` (ScriptableObject)
  - Main source selector (`XlsxRelativePath`, `RuntimeAsset`, `PreferRuntimeAsset`).
  - Resolves workbook absolute path from StreamingAssets.
  - Loads model from runtime asset when preferred, otherwise from XLSX.
- `SlotMathLoader`
  - Reads workbook sheets and builds validated `SlotMathModel`.
  - Required sheets: `Symbols`, `ReelStrips`, `Paytable`.
  - Optional sheets: `BonusPaytable`, `Config`.
- `XlsxSheetReader`
  - Reads workbook into sheet row dictionaries.
- `SheetParserHelpers`
  - Column requirements and typed/validated extraction helpers.
- `SlotMathRuntimeAsset` (ScriptableObject)
  - Stores serialized math JSON plus source path/hash/time metadata.

### Workbook contract (current)
- `Symbols`: `SymbolId`, `Code`, `PrefabKey`, optional flags.
- `ReelStrips`: `ReelIndex`, `Order`, `SymbolId`.
- `Paytable`: `SymbolId`, `Count`, `Payout`.
- `BonusPaytable` (optional): `Count`, `Payout`.
- `Config` (optional): keys like `VisibleRows`, `BonusEligibleReelIndices`, `ReelCount`, `PayoutMode`.

### Failure behavior
- Invalid or missing required data throws `InvalidDataException`.
- Production handling vs dev fallback is controlled higher up by `MachineController` + `RuntimeModeConfig`.

---

## 3) Spin Engine System (`Core/Engine`)

### Purpose
Resolves reel stops and evaluates payout/features from a math model + RNG provider.

### Small components
- `SlotEngine`
  - Orchestrator for resolve + payout evaluate.
  - Supports allocating and reusable-buffer spin calls.
- `SpinResolver`
  - Sorts reels by `ReelIndex`.
  - Uses RNG stop index per reel and fills landed symbol matrix for visible rows.
- `PayoutCalculator`
  - Evaluates:
    1. Primary mode payout (currently center-line).
    2. Scatter-style payouts (symbols marked `IsScatter` or `IsBonus`).
    3. Optional bonus paytable payout.
  - Populates details (`LineWins`, `ScatterWins`, `TriggeredFeatures`) when requested.
- `SpinResult`
  - Runtime result object containing reel stops, landed matrix, payouts, and feature flags.
- `RNGProvider` / `SeededRNGProvider`
  - RNG abstraction and deterministic seeded implementation.

### Triggered feature contract
Current payout pass sets feature keys:
- `bonus_anticipation` when bonus count >= 2.
- `bonus_triggered` when bonus count >= 3.

Presentation scripts consume these keys.

---

## 4) Session/Economy System (`Core/GameSession`)

### Purpose
Maintains integer-backed economy and spin transaction lifecycle.

### Small components
- `PlayerWallet`
  - Stores `Balance`, handles `TryDeduct` and `Credit`.
- `BetConfig`
  - Stores `TotalBet` (clamped non-negative).
- `SpinTransaction`
  - Start-spin response (`Accepted`, `BetAmount`, `BalanceAfterDeduct`).
- `SpinSettlement`
  - End-spin settlement (`Payout`, `BalanceAfterSettle`).
- `SlotGameSession`
  - Coordinates bet config, start-spin deduct, and settlement credit.

### Runtime flow
1. `TryStartSpin` deducts bet and returns acceptance.
2. Game logic resolves spin and payout.
3. `SettleSpin` credits payout and returns final balance.

---

## 5) Simulation System (`Core/Simulation`)

### Purpose
Runs high-volume deterministic simulations for balancing and verification.

### Small components
- `SlotSimulationRequest`
  - Input payload (spins, seed, model, buckets, optional CSV export config).
- `SlotSimulationRunner`
  - Executes spins with reusable buffers for performance.
  - Computes RTP, hit frequency, win distribution, landing frequencies.
  - Exports CSV when enabled.
- `SlotSimulationReport`
  - Output aggregate report object.
- `WinDistributionBucket`
  - Payout range bin definition and spin counter.
- `SymbolLandingFrequencyRow`
  - Per reel/row symbol hit map.

### Typical usage
Used by editor tooling (`SlotSimulationWindow`) to run simulations directly from a chosen `SlotMathConfig`.

---

## 6) Runtime Mode System (`Core/Runtime`)

### Purpose
Controls startup strictness and development fallback behavior.

### Small components
- `RuntimeModeConfig` (ScriptableObject)
  - `IsProductionMode`.
  - `AllowLegacyFallbackInDevelopment`.

### Effect on startup
- Production mode: startup fails fast on math configuration/load failures.
- Development mode: can fallback to `DefaultSlotMathModel` and optionally allow legacy reel-stop symbol fallback.
