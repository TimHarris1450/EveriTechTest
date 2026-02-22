# Slot Math Workbook Format

`SlotMathLoader` expects an `.xlsx` workbook with the following sheets.

> Note: `SlotMathTemplate.xlsx` in this repository is a **text-only template** because binary files are not supported in this review flow.
> Build a real workbook by creating sheets with the same names and columns below, then save/export as `.xlsx` before running in Unity.

## Required sheets

### 1) `Symbols`
| Column | Required | Notes |
|---|---|---|
| `SymbolId` | Yes | Integer id used everywhere else (`>= 0`). |
| `Code` | Yes | Human-readable symbol code. |
| `PrefabKey` | Yes | Prefab lookup key used by presentation. |
| `IsWild` | No | `true/false` or `1/0`. |
| `IsScatter` | No | `true/false` or `1/0`. |
| `IsBonus` | No | `true/false` or `1/0`. |

### 2) `ReelStrips`
| Column | Required | Notes |
|---|---|---|
| `ReelIndex` | Yes | Zero-based reel index (`>= 0`). |
| `Order` | Yes | Sort position within reel strip (`>= 0`, unique per reel). |
| `SymbolId` | Yes | Must exist in `Symbols.SymbolId`. |

### 3) `Paytable`
| Column | Required | Notes |
|---|---|---|
| `SymbolId` | Yes | Must exist in `Symbols.SymbolId`. |
| `Count` | Yes | Match count (`>= 1`). |
| `Payout` | Yes | Payout amount (`>= 0`). |

## Optional sheet

### `Config`
| Column | Required | Notes |
|---|---|---|
| `Key` | Yes | Config key name. |
| `Value` | Yes | Config value. |

Supported keys:
- `VisibleRows` (integer > 0)
- `BonusEligibleReelIndices` (comma-separated unique integers, e.g. `1,2,3`)
- `ReelCount` (integer, validated against unique `ReelIndex` count)

## Validation behavior
The loader throws descriptive errors when:
- Required sheets or columns are missing.
- `ReelStrips` or `Paytable` reference unknown `SymbolId` values.
- A reel strip is empty.
- Reel rows contain invalid or duplicate strip orders.
- Paytable rows have invalid values or duplicate `(SymbolId, Count)` entries.
