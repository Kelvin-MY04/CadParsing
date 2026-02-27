using System.Runtime.Serialization;

namespace CadParsing.Configuration
{
    [DataContract]
    public sealed class AppConfig
    {
        [DataMember] public string BorderLayerSuffix { get; set; } = "$0$PAPER-EX";
        [DataMember] public string TextLayerSuffix { get; set; } = "TEX";
        [DataMember] public double FloorPlanTextHeight { get; set; } = 400.0;
        [DataMember] public double TextHeightTolerance { get; set; } = 0.5;
        [DataMember] public bool AcceptClosedPolylinesOnly { get; set; } = true;
        [DataMember] public string DownloadRoot { get; set; } = @"C:\Users\pphyo\Downloads";
        [DataMember] public string ExportRoot { get; set; } = @"C:\Users\pphyo\Downloads\export\pdf";
    }
}
