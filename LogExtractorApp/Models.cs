using System;

namespace LogExtractorApp
{
    public class TestCaseInfo
    {
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string SafeFileName => PathUtils.SanitizeFileName($"CASE_{CaseId}_{Title}.log");
    }
}
