using CadParsing.Configuration;

namespace CadParsing
{
    internal static class Constants
    {
        public static string BorderLayerPattern =>
            "*" + ConfigLoader.Instance.BorderLayerSuffix;

        public static string TextLayerPattern =>
            "*" + ConfigLoader.Instance.TextLayerSuffix;

        public const string BlockSuffix = "\uc2dc\ud2b8";

        public static double TextHeight =>
            ConfigLoader.Instance.FloorPlanTextHeight;

        public static double HeightTolerance =>
            ConfigLoader.Instance.TextHeightTolerance;

        public static string DownloadRoot =>
            ConfigLoader.Instance.DownloadRoot;

        public static string ExportRoot =>
            ConfigLoader.Instance.ExportRoot;
    }
}
