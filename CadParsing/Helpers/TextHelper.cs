using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace CadParsing.Helpers
{
    internal static class TextHelper
    {
        public static SelectionFilter CreateTextFilter()
        {
            return new SelectionFilter(new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator,  "<AND"),
                new TypedValue((int)DxfCode.Operator,  "<OR"),
                new TypedValue((int)DxfCode.Start,     "TEXT"),
                new TypedValue((int)DxfCode.Start,     "MTEXT"),
                new TypedValue((int)DxfCode.Operator,  "OR>"),
                new TypedValue((int)DxfCode.LayerName,  Constants.TextLayerPattern),
                new TypedValue((int)DxfCode.Operator,  "AND>")
            });
        }

        public static bool MatchesTargetHeight(double height)
        {
            return Math.Abs(height - Constants.TextHeight) <= Constants.HeightTolerance;
        }

        public static string GetTextStyleName(Transaction transaction, ObjectId textStyleId)
        {
            if (textStyleId.IsNull) return "";
            TextStyleTableRecord styleRecord =
                transaction.GetObject(textStyleId, OpenMode.ForRead) as TextStyleTableRecord;
            return styleRecord?.Name ?? "";
        }

        public static string FindFloorPlanName(
            Editor editor, Transaction transaction, Extents3d borderExtents)
        {
            SelectionSet matchingTexts = SelectTextsInRegion(editor, borderExtents);
            if (matchingTexts == null)
                return null;

            string floorPlanName = FindLargestMatchingText(transaction, matchingTexts);

            return string.IsNullOrEmpty(floorPlanName) ? null : floorPlanName.Trim();
        }

        private static SelectionSet SelectTextsInRegion(
            Editor editor, Extents3d regionExtents)
        {
            PromptSelectionResult selectionResult = editor.SelectCrossingWindow(
                regionExtents.MinPoint, regionExtents.MaxPoint, CreateTextFilter());

            if (selectionResult.Status != PromptStatus.OK
                || selectionResult.Value == null
                || selectionResult.Value.Count == 0)
                return null;

            return selectionResult.Value;
        }

        private static string FindLargestMatchingText(
            Transaction transaction, SelectionSet textEntities)
        {
            double largestHeight = double.MinValue;
            string largestText = null;

            foreach (SelectedObject selectedObject in textEntities)
            {
                if (selectedObject == null) continue;

                Entity entity = transaction.GetObject(
                    selectedObject.ObjectId, OpenMode.ForRead) as Entity;
                if (entity == null) continue;

                ExtractTextInfo(entity, out double height, out string textValue);

                if (!MatchesTargetHeight(height))
                    continue;

                if (height > largestHeight)
                {
                    largestHeight = height;
                    largestText = textValue;
                }
            }

            return largestText;
        }

        public static void ExtractTextInfo(
            Entity entity, out double height, out string textValue)
        {
            height = 0;
            textValue = null;

            if (entity is DBText singleLineText)
            {
                height = singleLineText.Height;
                textValue = singleLineText.TextString;
            }
            else if (entity is MText multiLineText)
            {
                height = multiLineText.TextHeight;
                textValue = multiLineText.Contents;
            }
        }
    }
}
