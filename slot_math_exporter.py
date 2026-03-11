#!/usr/bin/env python3
"""Convert a professional slot math workbook into SlotMathRuntime.xlsx."""

from __future__ import annotations

import argparse
import logging
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import pandas as pd

LOGGER = logging.getLogger("slot_math_exporter")


class ExportValidationError(ValueError):
    """Raised when workbook contents fail validation."""


@dataclass(frozen=True)
class ParsedTemplate:
    reel_counts: pd.DataFrame
    paytable: pd.DataFrame
    pay_count: int
    bonus_paytable: pd.DataFrame | None = None


def configure_logging(verbose: bool) -> None:
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(level=level, format="%(levelname)s: %(message)s")


def normalize_column_name(name: Any) -> str:
    return re.sub(r"[^a-z0-9]+", "", str(name).strip().lower())


def normalize_symbol(value: Any) -> str:
    return str(value).strip()


def clean_dataframe(df: pd.DataFrame) -> pd.DataFrame:
    clean = df.dropna(how="all").copy()
    clean.columns = [str(col).strip() for col in clean.columns]
    return clean


def load_workbook(input_path: Path) -> dict[str, pd.DataFrame]:
    LOGGER.info("Loading workbook: %s", input_path)
    sheets = pd.read_excel(input_path, sheet_name=None, engine="openpyxl")
    return {name: clean_dataframe(df) for name, df in sheets.items()}


def find_reel_counts_sheet(sheets: dict[str, pd.DataFrame]) -> pd.DataFrame:
    required = {"reel", "symbol", "stops"}
    for name, df in sheets.items():
        normalized = {normalize_column_name(col): col for col in df.columns}
        if required.issubset(normalized):
            LOGGER.info("Using sheet '%s' as reel symbol counts", name)
            reel_df = df[[normalized["reel"], normalized["symbol"], normalized["stops"]]].copy()
            reel_df.columns = ["Reel", "Symbol", "Stops"]
            return reel_df
    raise ExportValidationError("Could not find reel counts section with columns: Reel, Symbol, Stops")


def parse_pay_column(columns: list[str]) -> tuple[str, int]:
    normalized = {normalize_column_name(col): col for col in columns}
    pay_candidates = [col for key, col in normalized.items() if key.startswith("pay")]
    if not pay_candidates:
        raise ExportValidationError("Could not find paytable payout column (expected Pay_3OAK or similar)")

    pay_col = pay_candidates[0]
    header_norm = normalize_column_name(pay_col)
    match = re.search(r"(\d+)", header_norm)
    count = int(match.group(1)) if match else 3
    return pay_col, count


def find_paytable_sheet(sheets: dict[str, pd.DataFrame]) -> tuple[pd.DataFrame, int]:
    for name, df in sheets.items():
        normalized = {normalize_column_name(col): col for col in df.columns}
        if "symbol" not in normalized:
            continue
        try:
            pay_col, count = parse_pay_column(list(df.columns))
        except ExportValidationError:
            continue
        LOGGER.info("Using sheet '%s' as paytable", name)
        pay_df = df[[normalized["symbol"], pay_col]].copy()
        pay_df.columns = ["Symbol", "Payout"]
        return pay_df, count
    raise ExportValidationError("Could not find paytable section with Symbol and Pay_* columns")


def find_bonus_paytable_sheet(sheets: dict[str, pd.DataFrame]) -> pd.DataFrame | None:
    required = {"count", "payout"}
    for name, df in sheets.items():
        normalized = {normalize_column_name(col): col for col in df.columns}
        if required.issubset(normalized) and "symbol" not in normalized:
            LOGGER.info("Using sheet '%s' as optional bonus paytable", name)
            bonus_df = df[[normalized["count"], normalized["payout"]]].copy()
            bonus_df.columns = ["Count", "Payout"]
            return bonus_df
    return None


def parse_template(sheets: dict[str, pd.DataFrame]) -> ParsedTemplate:
    reel_counts = find_reel_counts_sheet(sheets)
    paytable, pay_count = find_paytable_sheet(sheets)
    bonus_paytable = find_bonus_paytable_sheet(sheets)
    return ParsedTemplate(reel_counts=reel_counts, paytable=paytable, pay_count=pay_count, bonus_paytable=bonus_paytable)


def parse_reel_index(value: Any) -> int:
    if pd.isna(value):
        raise ExportValidationError("Found reel row with missing reel identifier")
    text = str(value).strip()
    match = re.search(r"(\d+)", text)
    if match:
        return int(match.group(1)) - 1
    if text.isdigit():
        return int(text) - 1
    raise ExportValidationError(f"Could not parse reel number from value: {value}")


def validate_and_prepare(parsed: ParsedTemplate) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame | None]:
    reels = parsed.reel_counts.copy()
    pays = parsed.paytable.copy()

    reels["Symbol"] = reels["Symbol"].map(normalize_symbol)
    reels["Stops"] = pd.to_numeric(reels["Stops"], errors="coerce")
    reels["ReelIndex"] = reels["Reel"].map(parse_reel_index)

    if reels["Symbol"].eq("").any() or reels["Symbol"].isna().any():
        raise ExportValidationError("Missing symbol found in reel counts")
    if reels["Stops"].isna().any() or (reels["Stops"] <= 0).any():
        raise ExportValidationError("Reel missing stops or has non-positive stops")

    pays["Symbol"] = pays["Symbol"].map(normalize_symbol)
    pays["Payout"] = pd.to_numeric(pays["Payout"], errors="coerce")

    if pays["Symbol"].eq("").any() or pays["Symbol"].isna().any():
        raise ExportValidationError("Missing symbol found in paytable")
    if pays["Payout"].isna().any() or (pays["Payout"] < 0).any():
        raise ExportValidationError("Paytable contains invalid payout values")

    bonus = parsed.bonus_paytable.copy() if parsed.bonus_paytable is not None else None
    if bonus is not None:
        bonus["Count"] = pd.to_numeric(bonus["Count"], errors="coerce")
        bonus["Payout"] = pd.to_numeric(bonus["Payout"], errors="coerce")
        bonus = bonus.dropna(how="all")
        if bonus.empty:
            bonus = None
        elif bonus["Count"].isna().any() or bonus["Payout"].isna().any():
            raise ExportValidationError("Bonus paytable contains invalid numeric values")

    return reels, pays, bonus


def build_symbols(reels: pd.DataFrame, pays: pd.DataFrame) -> tuple[pd.DataFrame, dict[str, int]]:
    ordered_symbols: list[str] = []
    for symbol in pd.concat([reels["Symbol"], pays["Symbol"]], ignore_index=True):
        if symbol not in ordered_symbols:
            ordered_symbols.append(symbol)

    symbol_ids = {symbol: idx for idx, symbol in enumerate(ordered_symbols)}
    if len(symbol_ids) != len(ordered_symbols):
        raise ExportValidationError("Duplicate symbol id detected")

    symbols_df = pd.DataFrame(
        {
            "SymbolId": [symbol_ids[s] for s in ordered_symbols],
            "Code": ordered_symbols,
            "PrefabKey": ordered_symbols,
            "IsWild": [s.upper() == "WILD" for s in ordered_symbols],
            "IsScatter": [s.upper() == "SCATTER" for s in ordered_symbols],
            "IsBonus": [s.upper() == "BONUS" for s in ordered_symbols],
        }
    )
    return symbols_df, symbol_ids


def build_reel_strips(reels: pd.DataFrame, symbol_ids: dict[str, int]) -> pd.DataFrame:
    records: list[dict[str, int]] = []
    for reel_index, group in reels.sort_values(["ReelIndex"]).groupby("ReelIndex", sort=True):
        order = 0
        for _, row in group.iterrows():
            symbol = row["Symbol"]
            if symbol not in symbol_ids:
                raise ExportValidationError(f"Reel references unknown symbol: {symbol}")
            stops = int(row["Stops"])
            for _ in range(stops):
                records.append(
                    {
                        "ReelIndex": int(reel_index),
                        "Order": order,
                        "SymbolId": symbol_ids[symbol],
                    }
                )
                order += 1
        if order == 0:
            raise ExportValidationError(f"Reel {reel_index + 1} missing stops")

    return pd.DataFrame(records)


def build_paytable(pays: pd.DataFrame, pay_count: int, symbol_ids: dict[str, int]) -> pd.DataFrame:
    rows: list[dict[str, int]] = []
    for _, row in pays.iterrows():
        symbol = row["Symbol"]
        if symbol not in symbol_ids:
            raise ExportValidationError(f"Paytable references unknown symbol: {symbol}")
        rows.append(
            {
                "SymbolId": symbol_ids[symbol],
                "Count": pay_count,
                "Payout": int(row["Payout"]),
            }
        )
    return pd.DataFrame(rows)


def build_config(reel_strips: pd.DataFrame) -> pd.DataFrame:
    reel_count = int(reel_strips["ReelIndex"].nunique())
    return pd.DataFrame(
        {
            "Key": ["ReelCount", "VisibleRows"],
            "Value": [reel_count, 3],
        }
    )


def write_runtime_workbook(
    output_path: Path,
    symbols_df: pd.DataFrame,
    reel_strips_df: pd.DataFrame,
    paytable_df: pd.DataFrame,
    config_df: pd.DataFrame,
    bonus_paytable_df: pd.DataFrame | None,
) -> None:
    LOGGER.info("Writing runtime workbook: %s", output_path)
    with pd.ExcelWriter(output_path, engine="openpyxl") as writer:
        symbols_df.to_excel(writer, sheet_name="Symbols", index=False)
        reel_strips_df.to_excel(writer, sheet_name="ReelStrips", index=False)
        paytable_df.to_excel(writer, sheet_name="Paytable", index=False)
        if bonus_paytable_df is not None:
            bonus_paytable_df.to_excel(writer, sheet_name="BonusPaytable", index=False)
        config_df.to_excel(writer, sheet_name="Config", index=False)


def print_summary(symbols_df: pd.DataFrame, reel_strips_df: pd.DataFrame, paytable_df: pd.DataFrame) -> None:
    stops_per_reel = reel_strips_df.groupby("ReelIndex").size().to_dict()
    print("Export Summary")
    print(f"Symbols: {len(symbols_df)}")
    print(f"Reels: {len(stops_per_reel)}")
    print(f"Total stops per reel: {stops_per_reel}")
    print(f"Paytable entries: {len(paytable_df)}")


def export_slot_math(input_path: Path, output_path: Path) -> None:
    sheets = load_workbook(input_path)
    parsed = parse_template(sheets)
    reels, pays, bonus = validate_and_prepare(parsed)

    symbols_df, symbol_ids = build_symbols(reels, pays)
    reel_strips_df = build_reel_strips(reels, symbol_ids)
    paytable_df = build_paytable(pays, parsed.pay_count, symbol_ids)
    config_df = build_config(reel_strips_df)

    write_runtime_workbook(output_path, symbols_df, reel_strips_df, paytable_df, config_df, bonus)
    print_summary(symbols_df, reel_strips_df, paytable_df)


def build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Convert slot math template workbook to runtime workbook")
    parser.add_argument("input", nargs="?", default="Slot_Math_Professional_Template.xlsx", help="Input template path")
    parser.add_argument("output", nargs="?", default="SlotMathRuntime.xlsx", help="Output runtime workbook path")
    parser.add_argument("--verbose", action="store_true", help="Enable debug logging")
    return parser


def main() -> int:
    parser = build_arg_parser()
    args = parser.parse_args()
    configure_logging(args.verbose)

    input_path = Path(args.input)
    output_path = Path(args.output)

    try:
        export_slot_math(input_path, output_path)
    except FileNotFoundError:
        LOGGER.error("Input file not found: %s", input_path)
        return 1
    except ExportValidationError as exc:
        LOGGER.error("Validation failed: %s", exc)
        return 2
    except Exception as exc:  # noqa: BLE001
        LOGGER.exception("Unexpected failure: %s", exc)
        return 3
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
