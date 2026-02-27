namespace CadParsing.Helpers
{
    public static class BoundsChecker
    {
        public static bool IsInsideBounds(
            double x, double y,
            double minX, double minY, double maxX, double maxY)
        {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }
    }
}
