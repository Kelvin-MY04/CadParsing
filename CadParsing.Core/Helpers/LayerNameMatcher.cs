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

        public static bool MatchesAnyLayerSuffix(string layerName, string[] suffixes)
        {
            if (string.IsNullOrEmpty(layerName) || suffixes == null || suffixes.Length == 0)
                return false;

            foreach (string suffix in suffixes)
            {
                if (MatchesLayerSuffix(layerName, suffix))
                    return true;
            }

            return false;
        }
    }
}
