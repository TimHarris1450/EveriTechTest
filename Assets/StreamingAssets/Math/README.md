# Slot Math Workbook Format

`SlotMathLoader` expects an `.xlsx` workbook with the following sheets.


> Note: This repository keeps `SlotMathTemplate.xlsx` as a text-only template to avoid binary-file restrictions in this environment.
> Build a real workbook by creating sheets with the same names and columns, then save/export as `.xlsx` before using it with `SlotMathLoader`.

## Required sheets

### 1) `Symbols`
| Column | Required | Notes |
|---|---|---|
| `SymbolId` | Yes | Integer id used everywhere else. |
| `Code` | Yes | Human-readable symbol code. |
| `PrefabKey` | Yes | Prefab lookup key used by presentation. |
| `IsWild` | No | `true/false` or `1/0`. |
| `IsScatter` | No | `true/false` or `1/0`. |
| `IsBonus` | No | `true/false` or `1/0`. |

### 2) `ReelStrips`
| Column | Required | Notes |
|---|---|---|
| `ReelIndex` | Yes | Zero-based reel index. |
| `Order` | Yes | Sort position within reel strip. |
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
- `BonusEligibleReelIndices` (comma-separated integers, e.g. `1,2,3`)
- `ReelCount` (integer, validated against unique `ReelIndex` count)

## Validation behavior
The loader throws descriptive errors when:
- Required sheets or columns are missing.
- `ReelStrips` or `Paytable` reference unknown `SymbolId` values.
- A reel strip is empty.
- Paytable rows have invalid values.
