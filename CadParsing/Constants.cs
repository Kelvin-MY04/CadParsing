using CadParsing.Configuration;

namespace CadParsing
{
    internal static class Constants
    {
        public static string BorderLayerPattern =>
            "*" + ConfigLoader.Instance.BorderLayerSuffix;

        //public const string BlockSuffix = "\uc2dc\ud2b8";
        //public const string BlockSuffix = "\uD3C9\uD0DD\uD654\uC591\uC9C0\uAD6C\u0020\uB3C4\uBA74\uD3FC";
        public const string BlockSuffix = "Usan-Sheet";

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
