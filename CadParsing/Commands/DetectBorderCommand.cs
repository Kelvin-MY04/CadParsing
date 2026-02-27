using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadParsing.Configuration;
using CadParsing.Helpers;

namespace CadParsing.Commands
{
    public class DetectBorderCommand
    {
        [CommandMethod("DETECTBORDER")]
        public void DetectBorder()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document?.Editor;
            if (editor == null) return;

            Database database = document.Database;

            List<KeyValuePair<ObjectId, double>> detectedBorders =
                BorderHelper.FindBordersInModelSpace(database);

            if (detectedBorders.Count == 0)
            {
                editor.WriteMessage("\n[WARN] No border detected on target layer.");
                editor.WriteMessage("\n");
                return;
            }

            editor.WriteMessage(string.Format(
                "\n[INFO] {0} border(s) detected (by area).", detectedBorders.Count));

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                for (int borderIndex = 0; borderIndex < detectedBorders.Count; borderIndex++)
                {
                    ObjectId borderId = detectedBorders[borderIndex].Key;
                    double boundingBoxArea = detectedBorders[borderIndex].Value;

                    Entity borderEntity = transaction.GetObject(
                        borderId, OpenMode.ForRead) as Entity;
                    Extents3d borderExtents = borderEntity.GeometricExtents;

                    PrintBorderHeader(editor, borderIndex, detectedBorders.Count,
                        borderEntity, boundingBoxArea, borderExtents);
                    PrintVertices(editor, transaction, borderEntity);
                    PrintTextsInBorder(editor, transaction, database, borderExtents);

                    editor.WriteMessage("\n=====================================================");
                }

                transaction.Commit();
            }

            editor.WriteMessage("\n");
        }

        private static void PrintBorderHeader(
            Editor editor, int borderIndex, int totalBorders,
            Entity borderEntity, double boundingBoxArea, Extents3d extents)
        {
            editor.WriteMessage("\n=====================================================");
            editor.WriteMessage(string.Format(
                "\n BORDER {0} of {1}", borderIndex + 1, totalBorders));
            editor.WriteMessage("\n=====================================================");
            editor.WriteMessage("\n  Handle    : " + borderEntity.Handle);
            editor.WriteMessage("\n  Layer     : " + borderEntity.Layer);
            editor.WriteMessage("\n  Type      : " + borderEntity.GetRXClass().Name);
            editor.WriteMessage(string.Format("\n  BBox Area : {0:F4}", boundingBoxArea));
            editor.WriteMessage(string.Format(
                "\n  Min       : ({0:F4},  {1:F4})",
                extents.MinPoint.X, extents.MinPoint.Y));
            editor.WriteMessage(string.Format(
                "\n  Max       : ({0:F4},  {1:F4})",
                extents.MaxPoint.X, extents.MaxPoint.Y));
            editor.WriteMessage("\n-----------------------------------------------------");
        }

        private static void PrintVertices(
            Editor editor, Transaction transaction, Entity borderEntity)
        {
            if (borderEntity is Polyline polyline)
                PrintPolylineVertices(editor, polyline);
            else if (borderEntity is Polyline2d polyline2d)
                PrintPolyline2dVertices(editor, transaction, polyline2d);
        }

        private static void PrintPolylineVertices(Editor editor, Polyline polyline)
        {
            Matrix3d ocsToWorldTransform = Matrix3d.WorldToPlane(polyline.Normal).Inverse();

            editor.WriteMessage(string.Format(
                "\n  Vertices  : {0}  |  Normal({1:F4}, {2:F4}, {3:F4})  |  Elevation: {4:F4}",
                polyline.NumberOfVertices,
                polyline.Normal.X, polyline.Normal.Y, polyline.Normal.Z,
                polyline.Elevation));
            editor.WriteMessage("\n-----------------------------------------------------");

            for (int vertexIndex = 0; vertexIndex < polyline.NumberOfVertices; vertexIndex++)
            {
                Point2d ocsPoint = polyline.GetPoint2dAt(vertexIndex);
                Point3d worldPoint = new Point3d(ocsPoint.X, ocsPoint.Y, polyline.Elevation)
                    .TransformBy(ocsToWorldTransform);

                editor.WriteMessage(string.Format(
                    "\n  [{0,3}]  OCS({1,12:F4}, {2,12:F4})" +
                    "  WCS({3,12:F4}, {4,12:F4}, {5,8:F4})",
                    vertexIndex, ocsPoint.X, ocsPoint.Y,
                    worldPoint.X, worldPoint.Y, worldPoint.Z));
            }
        }

        private static void PrintPolyline2dVertices(
            Editor editor, Transaction transaction, Polyline2d polyline2d)
        {
            Matrix3d ocsToWorldTransform = Matrix3d.WorldToPlane(polyline2d.Normal).Inverse();

            editor.WriteMessage(string.Format(
                "\n  Vertices (Polyline2d)  |  Normal({0:F4}, {1:F4}, {2:F4})",
                polyline2d.Normal.X, polyline2d.Normal.Y, polyline2d.Normal.Z));
            editor.WriteMessage("\n-----------------------------------------------------");

            int vertexIndex = 0;
            foreach (ObjectId vertexId in polyline2d)
            {
                Vertex2d vertex = transaction.GetObject(
                    vertexId, OpenMode.ForRead) as Vertex2d;
                if (vertex == null) continue;

                Point3d worldPoint = vertex.Position.TransformBy(ocsToWorldTransform);

                editor.WriteMessage(string.Format(
                    "\n  [{0,3}]  OCS({1,12:F4}, {2,12:F4})" +
                    "  WCS({3,12:F4}, {4,12:F4}, {5,8:F4})",
                    vertexIndex++, vertex.Position.X, vertex.Position.Y,
                    worldPoint.X, worldPoint.Y, worldPoint.Z));
            }
        }

        private static void PrintTextsInBorder(
            Editor editor, Transaction transaction,
            Database database, Extents3d borderExtents)
        {
            AppConfig config = ConfigLoader.Instance;

            editor.WriteMessage("\n-----------------------------------------------------");
            editor.WriteMessage(string.Format(
                "\n  Texts inside border ({0} layer, Height={1}):",
                config.TextLayerSuffix, config.FloorPlanTextHeight));
            editor.WriteMessage("\n-----------------------------------------------------");

            BlockTableRecord modelSpace =
                DatabaseHelper.GetModelSpaceBlock(transaction, database);

            int matchingTextCount = 0;

            foreach (ObjectId objectId in modelSpace)
            {
                Entity entity = TryOpenEntity(transaction, objectId);
                if (entity == null) continue;

                if (!LayerNameMatcher.MatchesLayerSuffix(entity.Layer, config.TextLayerSuffix))
                    continue;

                TextHelper.ExtractTextInfo(
                    entity, out double height, out string textValue, out Point3d insertionPoint);

                if (string.IsNullOrEmpty(textValue)) continue;

                if (!TextHelper.MatchesTargetHeight(height, config)) continue;

                if (!BoundsChecker.IsInsideBounds(
                        insertionPoint.X, insertionPoint.Y,
                        borderExtents.MinPoint.X, borderExtents.MinPoint.Y,
                        borderExtents.MaxPoint.X, borderExtents.MaxPoint.Y))
                    continue;

                matchingTextCount++;
                PrintTextEntity(editor, transaction, entity, matchingTextCount,
                    height, insertionPoint, textValue);
            }

            if (matchingTextCount == 0)
                editor.WriteMessage("\n  (no matching text found in border)");

            editor.WriteMessage(string.Format("\n  Total texts: {0}", matchingTextCount));
        }

        private static Entity TryOpenEntity(Transaction transaction, ObjectId objectId)
        {
            try
            {
                return transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static void PrintTextEntity(
            Editor editor, Transaction transaction, Entity entity,
            int count, double height, Point3d insertionPoint, string textValue)
        {
            if (entity is DBText singleLineText)
            {
                string styleName = TextHelper.GetTextStyleName(
                    transaction, singleLineText.TextStyleId);

                editor.WriteMessage(string.Format(
                    "\n  [{0,3}] TEXT  Handle: {1}  Layer: {2}",
                    count, entity.Handle, entity.Layer));
                editor.WriteMessage(string.Format(
                    "\n        Height  : {0:F4}  Rotation: {1:F4}\u00b0  Style: {2}",
                    height, singleLineText.Rotation * (180.0 / Math.PI), styleName));
                editor.WriteMessage(string.Format(
                    "\n        Position: ({0:F4}, {1:F4}, {2:F4})",
                    insertionPoint.X, insertionPoint.Y, insertionPoint.Z));
                editor.WriteMessage(string.Format("\n        Value   : {0}", textValue));
            }
            else if (entity is MText multiLineText)
            {
                string styleName = TextHelper.GetTextStyleName(
                    transaction, multiLineText.TextStyleId);

                editor.WriteMessage(string.Format(
                    "\n  [{0,3}] MTEXT Handle: {1}  Layer: {2}",
                    count, entity.Handle, entity.Layer));
                editor.WriteMessage(string.Format(
                    "\n        Height  : {0:F4}  Rotation: {1:F4}\u00b0  Style: {2}",
                    height, multiLineText.Rotation * (180.0 / Math.PI), styleName));
                editor.WriteMessage(string.Format(
                    "\n        Location: ({0:F4}, {1:F4}, {2:F4})",
                    insertionPoint.X, insertionPoint.Y, insertionPoint.Z));
                editor.WriteMessage(string.Format(
                    "\n        Width   : {0:F4}  Attachment: {1}",
                    multiLineText.Width, multiLineText.Attachment));
                editor.WriteMessage(string.Format("\n        Value   : {0}", textValue));
            }
        }
    }
}
