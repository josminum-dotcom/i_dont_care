using System.IO;
using System.Text.RegularExpressions;

namespace LogExtractorApp
{
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
}
