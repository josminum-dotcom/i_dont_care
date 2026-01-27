using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace LogExtractorCore
{
    public class TestCaseInfo
    {
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string SafeFileName => PathUtils.SanitizeFileName($"CASE_{CaseId}_{Title}.log");
    }

    public static class PathUtils
    {
        public static string SanitizeFileName(string fileName)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            // Replace invalid characters and whitespace with underscore
            string pattern = "[" + Regex.Escape(invalidChars) + @"\s]";
            string sanitized = Regex.Replace(fileName, pattern, "_");
            return sanitized;
        }
    }

    public class ExcelParser
    {
        public List<TestCaseInfo> Parse(string filePath)
        {
            var testCases = new List<TestCaseInfo>();
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet("테스트 결과서");
                var usedRange = worksheet.RangeUsed();
                if (usedRange == null) return testCases;

                int lastRow = usedRange.LastRow().RowNumber();

                for (int row = 1; row <= lastRow; row++)
                {
                    var cellB = worksheet.Cell(row, 2);
                    string cellValue = cellB.GetFormattedString();

                    if (cellValue.StartsWith("CASE", StringComparison.OrdinalIgnoreCase))
                    {
                        var tc = new TestCaseInfo();
                        tc.CaseId = cellValue.Replace("CASE", "").Trim();
                        tc.Title = worksheet.Cell(row, 4).GetFormattedString();

                        int dataRow = row + 2;
                        var timestamps = new List<DateTime>();

                        while (dataRow <= lastRow)
                        {
                            var noCell = worksheet.Cell(dataRow, 2);
                            if (string.IsNullOrWhiteSpace(noCell.GetFormattedString())) break;
                            if (noCell.GetFormattedString().StartsWith("CASE", StringComparison.OrdinalIgnoreCase)) break;

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
            try
            {
                if (value.Trim().StartsWith("{"))
                {
                    var obj = JObject.Parse(value);
                    dateStr = obj["date"]?.ToString();
                }
            }
            catch { }

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
                // Try with comma
                if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd HH:mm:ss,fff", null, System.Globalization.DateTimeStyles.None, out DateTime dt))
                {
                    return dt;
                }
                // Try with dot
                if (DateTime.TryParseExact(dateStr.Replace(',', '.'), "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out dt))
                {
                    return dt;
                }
                // Try without milliseconds just in case
                if (DateTime.TryParse(dateStr, out dt))
                {
                    return dt;
                }
            }

            return null;
        }
    }

    public class LogProcessor
    {
        public void Process(string logFilePath, List<TestCaseInfo> testCases, string outputFolder, IProgress<int>? progress)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var validCases = testCases.Where(c => c.StartTime.HasValue && c.EndTime.HasValue).ToList();
            if (validCases.Count == 0) return;

            var writers = new Dictionary<TestCaseInfo, StreamWriter>();
            try
            {
                foreach (var tc in validCases)
                {
                    string outputPath = Path.Combine(outputFolder, tc.SafeFileName);
                    Console.WriteLine($"Creating output file: {outputPath}");
                    writers[tc] = new StreamWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read));
                }

                long totalBytes = new FileInfo(logFilePath).Length;
                long processedBytes = 0;

                using (var reader = new StreamReader(new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        processedBytes += System.Text.Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

                        DateTime? logTime = ParseLogTime(line);
                        if (logTime.HasValue)
                        {
                            foreach (var tc in validCases)
                            {
                                if (logTime >= tc.StartTime && logTime <= tc.EndTime)
                                {
                                    writers[tc].WriteLine(line);
                                }
                            }
                        }

                        if (totalBytes > 0 && processedBytes % (1024 * 1024) < 2048)
                        {
                            int pct = (int)((processedBytes * 100) / totalBytes);
                            progress?.Report(Math.Min(pct, 100));
                        }
                    }
                }
            }
            finally
            {
                foreach (var writer in writers.Values)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
            progress?.Report(100);
        }

        public DateTime? ParseLogTime(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 23) return null;

            string timestampPart = line.Substring(0, 23);
            // Try with comma
            if (DateTime.TryParseExact(timestampPart, "yyyy-MM-dd HH:mm:ss,fff", null, System.Globalization.DateTimeStyles.None, out DateTime dt))
            {
                return dt;
            }
            // Try with dot
            if (DateTime.TryParseExact(timestampPart.Replace(',', '.'), "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out dt))
            {
                return dt;
            }

            return null;
        }
    }
}
