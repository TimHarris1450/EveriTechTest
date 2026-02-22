using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Scripts.Core.MathLoading
{
    internal static class XlsxSheetReader
    {
        public static Dictionary<string, List<Dictionary<string, string>>> ReadWorkbook(string path)
        {
            using ZipArchive archive = ZipFile.OpenRead(path);
            XNamespace mainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

            Dictionary<string, string> relationships = ReadRelationships(archive);
            List<string> sharedStrings = ReadSharedStrings(archive);

            XDocument workbook = LoadXml(archive, "xl/workbook.xml");
            XElement sheets = workbook.Root?.Element(mainNs + "sheets")
                ?? throw new InvalidDataException("Workbook is missing sheets.");

            Dictionary<string, List<Dictionary<string, string>>> result = new(StringComparer.OrdinalIgnoreCase);
            foreach (XElement sheet in sheets.Elements(mainNs + "sheet"))
            {
                string name = sheet.Attribute("name")?.Value;
                string relationshipId = sheet.Attribute(relNs + "id")?.Value;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relationshipId))
                {
                    continue;
                }

                if (!relationships.TryGetValue(relationshipId, out string targetPath))
                {
                    continue;
                }

                XDocument worksheet = LoadXml(archive, targetPath);
                result[name] = ParseWorksheetRows(worksheet, sharedStrings);
            }

            return result;
        }

        private static Dictionary<string, string> ReadRelationships(ZipArchive archive)
        {
            XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
            XDocument relDoc = LoadXml(archive, "xl/_rels/workbook.xml.rels");
            Dictionary<string, string> map = new();

            foreach (XElement relationship in relDoc.Root?.Elements(relNs + "Relationship") ?? Enumerable.Empty<XElement>())
            {
                string id = relationship.Attribute("Id")?.Value;
                string target = relationship.Attribute("Target")?.Value;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                string normalized = target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
                    ? target
                    : $"xl/{target.TrimStart('/')}";

                map[id] = normalized;
            }

            return map;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return new List<string>();
            }

            XNamespace mainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XDocument shared = LoadXml(archive, "xl/sharedStrings.xml");

            return shared.Root?
                .Elements(mainNs + "si")
                .Select(si => string.Concat(si.Descendants(mainNs + "t").Select(t => t.Value)))
                .ToList() ?? new List<string>();
        }

        private static List<Dictionary<string, string>> ParseWorksheetRows(XDocument worksheet, IReadOnlyList<string> sharedStrings)
        {
            XNamespace mainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XElement sheetData = worksheet.Root?.Element(mainNs + "sheetData");
            if (sheetData == null)
            {
                return new List<Dictionary<string, string>>();
            }

            List<Dictionary<string, string>> rows = new();
            string[] headers = null;

            foreach (XElement row in sheetData.Elements(mainNs + "row"))
            {
                Dictionary<int, string> cells = new();
                foreach (XElement cell in row.Elements(mainNs + "c"))
                {
                    string reference = cell.Attribute("r")?.Value;
                    if (string.IsNullOrWhiteSpace(reference))
                    {
                        continue;
                    }

                    int col = ColumnReferenceToIndex(reference);
                    cells[col] = ReadCellValue(cell, sharedStrings, mainNs);
                }

                if (headers == null)
                {
                    int maxHeaderIndex = cells.Keys.Count == 0 ? -1 : cells.Keys.Max();
                    headers = new string[maxHeaderIndex + 1];
                    foreach (KeyValuePair<int, string> kvp in cells)
                    {
                        headers[kvp.Key] = kvp.Value?.Trim();
                    }
                    continue;
                }

                Dictionary<string, string> mappedRow = new(StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < headers.Length; index++)
                {
                    string header = headers[index];
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    mappedRow[header] = cells.TryGetValue(index, out string value) ? value : string.Empty;
                }

                if (mappedRow.Count > 0 && mappedRow.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
                {
                    rows.Add(mappedRow);
                }
            }

            return rows;
        }

        private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings, XNamespace mainNs)
        {
            string type = cell.Attribute("t")?.Value;
            string raw = cell.Element(mainNs + "v")?.Value;

            if (type == "inlineStr")
            {
                return cell.Element(mainNs + "is")?.Element(mainNs + "t")?.Value ?? string.Empty;
            }

            if (type == "s" && int.TryParse(raw, out int sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
            {
                return sharedStrings[sharedIndex];
            }

            return raw ?? string.Empty;
        }

        private static int ColumnReferenceToIndex(string cellReference)
        {
            int index = 0;
            foreach (char character in cellReference)
            {
                if (!char.IsLetter(character))
                {
                    break;
                }

                index = (index * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
            }

            return index - 1;
        }

        private static XDocument LoadXml(ZipArchive archive, string path)
        {
            ZipArchiveEntry entry = archive.GetEntry(path)
                ?? throw new InvalidDataException($"Workbook is missing '{path}'.");

            using Stream stream = entry.Open();
            return XDocument.Load(stream);
        }
    }
}
