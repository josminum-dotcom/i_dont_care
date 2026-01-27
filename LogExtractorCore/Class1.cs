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
            string pattern = "[" + Regex.Escape(invalidChars) + @"\s]";
            string sanitized = Regex.Replace(fileName, pattern, "_");
            return sanitized;
        }
    }

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

    public class LogProcessor
    {
        public void Process(string logFolder, List<TestCaseInfo> testCases, string outputFolder, IProgress<string> logger, IProgress<int> progress)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var validCases = testCases.Where(c => c.StartTime.HasValue && c.EndTime.HasValue).ToList();
            if (validCases.Count == 0) return;

            int completedCases = 0;

            foreach (var tc in validCases)
            {
                logger?.Report($"CASE {tc.CaseId} 처리 중...");

                var relevantFiles = GetRelevantLogFiles(logFolder, tc.StartTime!.Value, tc.EndTime!.Value);

                if (relevantFiles.Count == 0)
                {
                    logger?.Report($"[경고] CASE {tc.CaseId}: 해당 시간대 로그 파일이 폴더에 없습니다.");
                    continue;
                }

                bool foundAny = false;
                string outputPath = Path.Combine(outputFolder, tc.SafeFileName);

                using (var writer = new StreamWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    foreach (var logFile in relevantFiles)
                    {
                        // Security Requirement: ReadAllBytes then MemoryStream
                        byte[] logData = File.ReadAllBytes(logFile);
                        using (var ms = new MemoryStream(logData))
                        using (var reader = new StreamReader(ms))
                        {
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                DateTime? logTime = ParseLogTime(line);
                                if (logTime.HasValue)
                                {
                                    if (logTime >= tc.StartTime && logTime <= tc.EndTime)
                                    {
                                        writer.WriteLine(line);
                                        foundAny = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (!foundAny)
                {
                    logger?.Report($"[알림] CASE {tc.CaseId}: 해당 구간의 로그를 찾을 수 없습니다.");
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
                else
                {
                    logger?.Report($"CASE {tc.CaseId}: 추출 완료.");
                }

                completedCases++;
                progress?.Report((completedCases * 100) / validCases.Count);
            }
        }

        private List<string> GetRelevantLogFiles(string folder, DateTime start, DateTime end)
        {
            if (!Directory.Exists(folder)) return new List<string>();

            var files = Directory.GetFiles(folder, "*.log");
            var relevant = new List<(string path, DateTime hour)>();

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                // Expected format: 설비명-yyyy-MM-dd-HH
                var match = Regex.Match(fileName, @"-(\d{4}-\d{2}-\d{2}-\d{2})$");
                if (match.Success)
                {
                    if (DateTime.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd-HH", null, System.Globalization.DateTimeStyles.None, out DateTime fileHour))
                    {
                        // Check for overlap: [fileHour, fileHour + 1) overlaps [start, end]
                        if (fileHour <= end && fileHour.AddHours(1) >= start)
                        {
                            relevant.Add((file, fileHour));
                        }
                    }
                }
            }

            return relevant.OrderBy(x => x.hour).Select(x => x.path).ToList();
        }

        public DateTime? ParseLogTime(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 23) return null;

            string timestampPart = line.Substring(0, 23);
            if (DateTime.TryParseExact(timestampPart, "yyyy-MM-dd HH:mm:ss,fff", null, System.Globalization.DateTimeStyles.None, out DateTime dt))
            {
                return dt;
            }
            if (DateTime.TryParseExact(timestampPart.Replace(',', '.'), "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out dt))
            {
                return dt;
            }

            return null;
        }
    }
}
