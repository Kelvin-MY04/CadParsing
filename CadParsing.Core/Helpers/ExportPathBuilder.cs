using System.IO;

namespace CadParsing.Helpers
{
    public static class ExportPathBuilder
    {
        public const string ColorPdfFolderName = "Color-PDF";
        public const string BwPdfFolderName = "BW-PDF";

        public static string BuildPdfPath(
            string drawingSubDirectory,
            string typeSubFolderName,
            string sanitizedBorderLabel)
        {
            return Path.Combine(drawingSubDirectory, typeSubFolderName, sanitizedBorderLabel + ".pdf");
        }

        public static void CreateTypeSubFolders(string drawingSubDirectory)
        {
            Directory.CreateDirectory(Path.Combine(drawingSubDirectory, ColorPdfFolderName));
            Directory.CreateDirectory(Path.Combine(drawingSubDirectory, BwPdfFolderName));
        }
    }
}
