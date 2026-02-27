using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadParsing.Configuration;

namespace CadParsing.Helpers
{
    internal static class TextHelper
    {
        public static string FindFloorPlanNameInModelSpace(
            Transaction transaction, Database database, Extents3d borderExtents)
        {
            try
            {
                AppConfig config = ConfigLoader.Instance;
                BlockTableRecord modelSpace =
                    DatabaseHelper.GetModelSpaceBlock(transaction, database);

                double bestHeight = double.MinValue;
                string bestText = null;

                foreach (ObjectId objectId in modelSpace)
                {
                    Entity entity = TryOpenEntity(transaction, objectId);
                    if (entity == null) continue;

                    if (!LayerNameMatcher.MatchesLayerSuffix(
                            entity.Layer, config.TextLayerSuffix))
                        continue;

                    ExtractTextInfo(
                        entity, out double height, out string textValue,
                        out Point3d insertionPoint);

                    if (height <= 0) continue;

                    if (string.IsNullOrEmpty(textValue)) continue;

                    if (!BoundsChecker.IsInsideBounds(
                            insertionPoint.X, insertionPoint.Y,
                            borderExtents.MinPoint.X, borderExtents.MinPoint.Y,
                            borderExtents.MaxPoint.X, borderExtents.MaxPoint.Y))
                        continue;

                    if (height > bestHeight)
                    {
                        bestHeight = height;
                        bestText = textValue;
                    }
                }

                if (bestText == null)
                {
                    Console.WriteLine(
                        "[WARN] TextHelper: No eligible TEX-layer text found inside border at ("
                        + borderExtents.MinPoint.X + ", " + borderExtents.MinPoint.Y + ")-("
                        + borderExtents.MaxPoint.X + ", " + borderExtents.MaxPoint.Y + ")");
                }

                return string.IsNullOrEmpty(bestText) ? null : bestText.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[ERROR] TextHelper.FindFloorPlanNameInModelSpace: " + ex.Message);
                return null;
            }
        }

        public static bool MatchesTargetHeight(double height, AppConfig config)
        {
            return Math.Abs(height - config.FloorPlanTextHeight) <= config.TextHeightTolerance;
        }

        public static void ExtractTextInfo(
            Entity entity, out double height, out string textValue, out Point3d insertionPoint)
        {
            height = 0;
            textValue = null;
            insertionPoint = Point3d.Origin;

            if (entity is DBText singleLineText)
            {
                height = singleLineText.Height;
                textValue = singleLineText.TextString;
                insertionPoint = singleLineText.Position;
            }
            else if (entity is MText multiLineText)
            {
                height = multiLineText.TextHeight;
                textValue = MTextFormatStripper.Strip(multiLineText.Contents);
                insertionPoint = multiLineText.Location;
            }
        }

        public static string GetTextStyleName(Transaction transaction, ObjectId textStyleId)
        {
            if (textStyleId.IsNull) return "";
            TextStyleTableRecord styleRecord =
                transaction.GetObject(textStyleId, OpenMode.ForRead) as TextStyleTableRecord;
            return styleRecord?.Name ?? "";
        }

        private static Entity TryOpenEntity(Transaction transaction, ObjectId objectId)
        {
            try
            {
                return transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
