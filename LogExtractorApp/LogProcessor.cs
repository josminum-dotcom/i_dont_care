using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogExtractorApp
{
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

            int processedCount = 0;

            foreach (var tc in validCases)
            {
                logger?.Report($"CASE {tc.CaseId} 처리 시작...");

                var relevantFiles = GetRelevantLogFiles(logFolder, tc.StartTime!.Value, tc.EndTime!.Value);

                if (relevantFiles.Count == 0)
                {
                    logger?.Report($"[경고] CASE {tc.CaseId}: 해당 시간 범위의 로그 파일을 찾을 수 없습니다.");
                    processedCount++;
                    progress?.Report((processedCount * 100) / validCases.Count);
                    continue;
                }

                bool foundAnyLine = false;
                string outputPath = Path.Combine(outputFolder, tc.SafeFileName);

                using (var writer = new StreamWriter(new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    foreach (var logFile in relevantFiles)
                    {
                        // Requirement: Read via byte array for security/AV bypass
                        byte[] logBytes = File.ReadAllBytes(logFile);
                        using (var ms = new MemoryStream(logBytes))
                        using (var reader = new StreamReader(ms))
                        {
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                DateTime? logTime = ParseLogTime(line);
                                if (logTime.HasValue && logTime >= tc.StartTime && logTime <= tc.EndTime)
                                {
                                    writer.WriteLine(line);
                                    foundAnyLine = true;
                                }
                            }
                        }
                    }
                }

                if (!foundAnyLine)
                {
                    logger?.Report($"[알림] CASE {tc.CaseId}: 해당 구간의 로그를 찾을 수 없습니다.");
                    if (File.Exists(outputPath)) File.Delete(outputPath);
                }
                else
                {
                    logger?.Report($"CASE {tc.CaseId}: 추출 완료.");
                }

                processedCount++;
                progress?.Report((processedCount * 100) / validCases.Count);
            }
        }

        private List<string> GetRelevantLogFiles(string folder, DateTime start, DateTime end)
        {
            var allFiles = Directory.GetFiles(folder, "*.log");
            var filtered = new List<(string path, DateTime hour)>();

            foreach (var file in allFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                // Machine-yyyy-MM-dd-HH
                var match = Regex.Match(name, @"-(\d{4}-\d{2}-\d{2}-\d{2})$");
                if (match.Success)
                {
                    if (DateTime.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd-HH", null, System.Globalization.DateTimeStyles.None, out DateTime fileHour))
                    {
                        // File covers [fileHour, fileHour + 1)
                        if (fileHour <= end && fileHour.AddHours(1) >= start)
                        {
                            filtered.Add((file, fileHour));
                        }
                    }
                }
            }

            return filtered.OrderBy(x => x.hour).Select(x => x.path).ToList();
        }

        public DateTime? ParseLogTime(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 23) return null;

            string tsPart = line.Substring(0, 23);
            if (DateTime.TryParseExact(tsPart, "yyyy-MM-dd HH:mm:ss,fff", null, System.Globalization.DateTimeStyles.None, out DateTime dt))
            {
                return dt;
            }
            if (DateTime.TryParseExact(tsPart.Replace(',', '.'), "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out dt))
            {
                return dt;
            }

            return null;
        }
    }
}
