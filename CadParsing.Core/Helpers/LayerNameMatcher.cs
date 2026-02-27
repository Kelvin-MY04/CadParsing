using System;

namespace CadParsing.Helpers
{
    public static class LayerNameMatcher
    {
        public static bool MatchesLayerSuffix(string layerName, string suffix)
        {
            if (string.IsNullOrEmpty(layerName) || string.IsNullOrEmpty(suffix))
                return false;

            return layerName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
