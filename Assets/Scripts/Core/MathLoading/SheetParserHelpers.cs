using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Scripts.Core.MathLoading
{
    internal static class SheetParserHelpers
    {
        public static void RequireColumns(string sheetName, IReadOnlyList<Dictionary<string, string>> rows, params string[] requiredColumns)
        {
            if (rows.Count == 0)
            {
                throw new InvalidDataException($"Sheet '{sheetName}' is missing header row or data.");
            }

            Dictionary<string, string> firstRow = rows[0];
            foreach (string requiredColumn in requiredColumns)
            {
                if (!firstRow.Keys.Any(key => string.Equals(key, requiredColumn, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidDataException($"Sheet '{sheetName}' is missing required column '{requiredColumn}'.");
                }
            }
        }

        public static string GetRequiredString(string sheetName, IReadOnlyDictionary<string, string> row, string key, int rowNumber)
        {
            if (!TryGetCaseInsensitive(row, key, out string value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException($"Sheet '{sheetName}' row {rowNumber} has empty required column '{key}'.");
            }

            return value.Trim();
        }

        public static int GetRequiredInt(string sheetName, IReadOnlyDictionary<string, string> row, string key, int rowNumber)
        {
            string raw = GetRequiredString(sheetName, row, key, rowNumber);
            if (!int.TryParse(raw, out int value))
            {
                throw new InvalidDataException($"Sheet '{sheetName}' row {rowNumber} column '{key}' expected integer but got '{raw}'.");
            }

            return value;
        }

        public static bool GetOptionalBool(string sheetName, IReadOnlyDictionary<string, string> row, string key, int rowNumber)
        {
            if (!TryGetCaseInsensitive(row, key, out string raw) || string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string normalized = raw.Trim();
            if (normalized == "1")
            {
                return true;
            }

            if (normalized == "0")
            {
                return false;
            }

            if (bool.TryParse(normalized, out bool parsed))
            {
                return parsed;
            }

            throw new InvalidDataException($"Sheet '{sheetName}' row {rowNumber} column '{key}' has invalid boolean '{raw}'. Expected true/false or 1/0.");
        }

        private static bool TryGetCaseInsensitive(IReadOnlyDictionary<string, string> row, string key, out string value)
        {
            foreach (KeyValuePair<string, string> pair in row)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
