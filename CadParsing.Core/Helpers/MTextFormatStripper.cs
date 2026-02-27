using System.Text.RegularExpressions;

namespace CadParsing.Helpers
{
    /// <summary>
    /// Strips AutoCAD MText formatting codes from raw MText.Contents strings,
    /// producing plain readable text suitable for use as a floor plan name.
    /// </summary>
    public static class MTextFormatStripper
    {
        private const string SemicolonCodePattern = @"\\[A-Za-z*'][^;]*;";
        private const string SingleCharCodePattern = @"\\[A-Za-z~]";

        /// <summary>
        /// Strips AutoCAD MText format codes from <paramref name="rawText"/> and returns
        /// plain text. Returns an empty string if <paramref name="rawText"/> is null or empty.
        /// </summary>
        public static string Strip(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
                return string.Empty;

            string result = StripSemicolonTerminatedCodes(rawText);
            result = StripSingleCharCodes(result);
            result = RemoveGroupBrackets(result);
            result = CollapseWhitespace(result);
            return result;
        }

        private static string StripSemicolonTerminatedCodes(string text)
        {
            return Regex.Replace(text, SemicolonCodePattern, string.Empty);
        }

        private static string StripSingleCharCodes(string text)
        {
            return Regex.Replace(text, SingleCharCodePattern, " ");
        }

        private static string RemoveGroupBrackets(string text)
        {
            return text.Replace("{", string.Empty).Replace("}", string.Empty);
        }

        private static string CollapseWhitespace(string text)
        {
            return Regex.Replace(text, @"\s+", " ").Trim();
        }
    }
}
