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
            if (excelData == null || excelData.Length == 0)
            {
                throw new ArgumentException("엑셀 데이터가 비어 있습니다.");
            }

            // Check for ZIP signature (0x50 0x4B 0x03 0x04) which is the start of an .xlsx file
            if (excelData.Length < 4 || excelData[0] != 0x50 || excelData[1] != 0x4B)
            {
                throw new Exception("파일이 올바른 엑셀(.xlsx) 형식이 아니거나 보안 프로그램에 의해 변조되었습니다. (.xls 파일은 지원하지 않습니다)");
            }

            var testCases = new List<TestCaseInfo>();

            try
            {
                using (var ms = new MemoryStream(excelData))
                {
                    ms.Position = 0;
                    // Standard constructor for Stream
                    using (var workbook = new XLWorkbook(ms))
                    {
                        var worksheet = workbook.Worksheet("테스트 결과서");
                        if (worksheet == null)
                        {
                            throw new Exception("'테스트 결과서' 시트를 찾을 수 없습니다.");
                        }

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
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex.Message.Contains("시트를 찾을 수 없습니다"))
                    throw;

                throw new Exception($"엑셀 파일을 읽는 중 오류가 발생했습니다. (파일이 열려 있거나 형식이 잘못되었을 수 있습니다)\n상세 내용: {ex.Message}", ex);
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
}
