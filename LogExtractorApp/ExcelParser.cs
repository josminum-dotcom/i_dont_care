using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ClosedXML.Excel;
using Newtonsoft.Json.Linq;

namespace LogExtractorApp
{
    public class ExcelParser
    {
        public List<TestCaseInfo> Parse(byte[] excelData)
        {
            var testCases = new List<TestCaseInfo>();
            using (var ms = new MemoryStream(excelData))
            using (var workbook = new XLWorkbook(ms))
            {
                var worksheet = workbook.Worksheet("테스트 결과서");
                var usedRange = worksheet.RangeUsed();
                if (usedRange == null) return testCases;

                int lastRow = usedRange.LastRow().RowNumber();

                for (int row = 1; row <= lastRow; row++)
                {
                    var cellB = worksheet.Cell(row, 2);
                    string cellValue = cellB.GetFormattedString();

                    // Search for "CASE 1", "CASE 1-1", etc.
                    if (cellValue.StartsWith("CASE", StringComparison.OrdinalIgnoreCase))
                    {
                        var tc = new TestCaseInfo();
                        tc.CaseId = cellValue.Replace("CASE", "").Trim();

                        // Title from D10:L10 equivalent (same row, column 4)
                        tc.Title = worksheet.Cell(row, 4).GetFormattedString();

                        int dataRow = row + 2;
                        var timestamps = new List<DateTime>();

                        while (dataRow <= lastRow)
                        {
                            var noCell = worksheet.Cell(dataRow, 2);
                            if (string.IsNullOrWhiteSpace(noCell.GetFormattedString())) break;
                            if (noCell.GetFormattedString().StartsWith("CASE", StringComparison.OrdinalIgnoreCase)) break;

                            // J column is 10
                            string jValue = worksheet.Cell(dataRow, 10).GetFormattedString();
                            DateTime? dt = ExtractTimestamp(jValue);
                            if (dt.HasValue)
                            {
                                timestamps.Add(dt.Value);
                            }
                            dataRow++;
                        }

                        if (timestamps.Count > 0)
                        {
                            tc.StartTime = timestamps.Min();
                            tc.EndTime = timestamps.Max();
                            testCases.Add(tc);
                        }
                        row = dataRow - 1;
                    }
                }
            }
            return testCases;
        }

        public DateTime? ExtractTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            string? dateStr = null;
            // Try JSON
            try
            {
                if (value.Trim().StartsWith("{"))
                {
                    var obj = JObject.Parse(value);
                    dateStr = obj["date"]?.ToString();
                }
            }
            catch { }

            // Try XML
            if (string.IsNullOrEmpty(dateStr))
            {
                try
                {
                    if (value.Trim().StartsWith("<"))
                    {
                        var doc = XDocument.Parse(value);
                        dateStr = doc.Root?.Element("date")?.Value ?? doc.Root?.Attribute("date")?.Value;
                    }
                }
                catch { }
            }

            // Fallback: Regex for yyyy-MM-dd HH:mm:ss,SSS
            if (string.IsNullOrEmpty(dateStr))
            {
                var match = Regex.Match(value, @"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2},\d{3}");
                if (match.Success)
                {
                    dateStr = match.Value;
                }
            }

            if (!string.IsNullOrEmpty(dateStr))
            {
                // yyyy-MM-dd HH:mm:ss,SSS (using fff for milliseconds in TryParseExact)
                if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd HH:mm:ss,fff", null, System.Globalization.DateTimeStyles.None, out DateTime dt))
                {
                    return dt;
                }
                if (DateTime.TryParseExact(dateStr.Replace(',', '.'), "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out dt))
                {
                    return dt;
                }
                if (DateTime.TryParse(dateStr, out dt))
                {
                    return dt;
                }
            }

            return null;
        }
    }
}
