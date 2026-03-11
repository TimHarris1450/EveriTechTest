# Gameplay & Presentation Systems Documentation

This folder holds the runtime scene orchestration and feature presentation scripts used by the current slot game flow.

## 1) Machine Orchestration System

### Main component
- `MachineController`

### Responsibility
Coordinates spin lifecycle from input -> reel animation -> engine resolution -> visual/application of outcomes -> wallet/meter updates.

### Small components and wiring
- **Scene references**
  - `Reels` root object with reel children.
  - Reel child dependencies: `ImageSetter`, `Animator`.
- **Injected gameplay systems**
  - `SlotMathConfig` for model loading.
  - `RuntimeModeConfig` for strict mode/fallback behavior.
  - `SlotEngine` (created at runtime).
  - `SlotGameSession` (created at runtime).
- **Feature/presentation systems**
  - `BonusTracker`.
  - `FlyUp`.
  - `MeterValue`.
  - `SymbolRegistry`.

### Runtime sequence
1. `Awake`
   - Loads math model.
   - Builds engine/session.
   - Caches dependencies.
   - Configures each reel `ImageSetter` with math + registry.
2. `SpinCheck`
   - If spinning, starts stop flow.
   - Otherwise attempts bet deduct (`TryStartSpin`), updates meter, then starts spin coroutine.
3. `StartSpin`
   - Fires reel spin triggers with stagger timing.
4. `StopSpin`
   - Gets `SpinResult` from engine.
   - Sends resolved symbols per reel to `ImageSetter`.
   - Fires stop triggers.
   - Settles payout and updates meter.
   - Applies bonus/fly-up feature reactions.

---

## 2) Symbol Swap System

### Main component
- `ImageSetter`

### Responsibility
Replaces visible symbols in reel slots according to resolved symbol IDs.

### Small components
- Symbol sources:
  - `Symbols` prefab list.
  - optional `_bonusSymbol` excluded from normal mapping.
- Runtime maps/caches:
  - symbol-id to `SymbolData`.
  - symbol-id to prefab.
  - optional pooled instances per prefab.
  - child-index to resolved symbol-id.
- Integration references:
  - `SymbolRegistry` prefab-key lookup.
  - `BonusTracker` and `SymbolAnimController` support.

### Notes
- Supports configured bonus-eligible reels.
- Uses fallback prefab logic if mapping is incomplete.
- Supports instance pooling to reduce allocations during rapid symbol swaps.

---

## 3) Bonus Feature Presentation System

### Main components
- `BonusTracker`
- `SymbolAnimController`
- `BlackHoleController`
- `FlyUp`

### Responsibility
Turns feature flags / bonus symbol landings into chained visual feedback.

### Small components and behavior
- `BonusTracker`
  - Collects landed bonus symbol animators.
  - Plays anticipation animation for 2 symbols.
  - Plays win animation + triggers black hole flow for 3+.
- `SymbolAnimController`
  - Singleton helper for animation triggers: hit/win/anticipation/idle.
- `BlackHoleController`
  - Runs alternating transition animation and can trigger meter count-up.
- `FlyUp`
  - Starts fly-up object motion when `bonus_triggered` is present.
  - Adds jackpot value when animation reaches target.

---

## 4) Meter & Utility Animation System

### Main components
- `MeterValue`
- `AnimPlayer`

### Responsibility
Displays/updates monetary values and supports simple animation trigger utilities.

### Small components
- `MeterValue`
  - Internal long-backed value.
  - `SetValue` for authoritative replacement.
  - `AddToValue` for incremental rewards.
  - `ApplySpinResult` for direct payout application.
  - `CountUp` coroutine with coin shower visual support.
- `AnimPlayer`
  - Generic wrapper to trigger a named animator trigger.

---

## 5) Symbol Registry System (`Presentation/`)

### Main component
- `SymbolRegistry`

### Responsibility
Provides prefab-key -> prefab lookup for math-driven symbol instantiation.

### Small components
- `SymbolPrefabBinding`
  - Serialized key/prefab pair.
- Internal dictionary cache (`_prefabsByKey`).
- `BuildFromPrefabs`
  - Merges explicit bindings and discovered prefabs by name.
- `TryGetPrefab`
  - Case-insensitive key lookup.

### Why it matters
This is the bridge between spreadsheet math definitions (`PrefabKey`) and concrete scene prefabs used by `ImageSetter`.
