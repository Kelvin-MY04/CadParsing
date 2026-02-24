using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace CadParsing.Helpers
{
    internal static class BorderHelper
    {
        public static List<KeyValuePair<ObjectId, double>> FindBorders(
            Database database, Editor editor)
        {
            SelectionFilter borderFilter = CreateBorderFilter();

            PromptSelectionResult selectionResult = editor.SelectAll(borderFilter);
            if (!HasResults(selectionResult))
                return new List<KeyValuePair<ObjectId, double>>();

            List<KeyValuePair<ObjectId, double>> borderCandidates =
                CollectClosedPolylineAreas(database, selectionResult.Value);

            borderCandidates.Sort((first, second) => second.Value.CompareTo(first.Value));

            return borderCandidates;
        }

        private static SelectionFilter CreateBorderFilter()
        {
            return new SelectionFilter(new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator,  "<AND"),
                new TypedValue((int)DxfCode.Operator,  "<OR"),
                new TypedValue((int)DxfCode.Start,     "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start,     "POLYLINE"),
                new TypedValue((int)DxfCode.Operator,  "OR>"),
                new TypedValue((int)DxfCode.LayerName,  Constants.BorderLayerPattern),
                new TypedValue((int)DxfCode.Operator,  "AND>")
            });
        }

        private static bool HasResults(PromptSelectionResult result)
        {
            return result.Status == PromptStatus.OK
                && result.Value != null
                && result.Value.Count > 0;
        }

        private static List<KeyValuePair<ObjectId, double>> CollectClosedPolylineAreas(
            Database database, SelectionSet selection)
        {
            var candidates = new List<KeyValuePair<ObjectId, double>>();

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selectedObject in selection)
                {
                    if (selectedObject == null) continue;

                    Entity entity = transaction.GetObject(
                        selectedObject.ObjectId, OpenMode.ForRead) as Entity;

                    if (entity == null || !IsClosedPolyline(entity))
                        continue;

                    double boundingBoxArea = TryGetBoundingBoxArea(entity);
                    if (boundingBoxArea > 0)
                        candidates.Add(new KeyValuePair<ObjectId, double>(
                            selectedObject.ObjectId, boundingBoxArea));
                }

                transaction.Commit();
            }

            return candidates;
        }

        private static bool IsClosedPolyline(Entity entity)
        {
            if (entity is Polyline polyline) return polyline.Closed;
            if (entity is Polyline2d polyline2d) return polyline2d.Closed;
            return false;
        }

        private static double TryGetBoundingBoxArea(Entity entity)
        {
            try
            {
                Extents3d extents = entity.GeometricExtents;
                double width = extents.MaxPoint.X - extents.MinPoint.X;
                double height = extents.MaxPoint.Y - extents.MinPoint.Y;
                return width * height;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
