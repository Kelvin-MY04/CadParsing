using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadParsing.Configuration;

namespace CadParsing.Helpers
{
    internal static class BorderHelper
    {
        public static List<KeyValuePair<ObjectId, double>> FindBordersInModelSpace(
            Database database)
        {
            var candidates = new List<KeyValuePair<ObjectId, double>>();

            try
            {
                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    BlockTableRecord modelSpace =
                        DatabaseHelper.GetModelSpaceBlock(transaction, database);

                    AppConfig config = ConfigLoader.Instance;

                    foreach (ObjectId objectId in modelSpace)
                    {
                        Entity entity = TryOpenEntity(transaction, objectId);
                        if (entity == null) continue;

                        if (!IsPolylineOnBorderLayer(entity, config.BorderLayerSuffix))
                        {
                            Console.WriteLine("[ERROR] No Polyline on boarder layer");
                            continue;
                        }

                        if (config.AcceptClosedPolylinesOnly && !IsClosedPolyline(entity))
                        {
                            Console.WriteLine(
                                "[ERROR] Polyline is not closed: " + entity.Handle);
                            continue;
                        }

                        double area = TryGetBoundingBoxArea(entity);
                        if (area > 0)
                            candidates.Add(new KeyValuePair<ObjectId, double>(objectId, area));
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[ERROR] BorderHelper.FindBordersInModelSpace: " + ex.Message);
            }

            candidates.Sort((a, b) => b.Value.CompareTo(a.Value));
            return candidates;
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

        private static bool IsPolylineOnBorderLayer(Entity entity, string borderLayerSuffix)
        {
            if (!(entity is Polyline) && !(entity is Polyline2d))
                return false;

            return LayerNameMatcher.MatchesLayerSuffix(entity.Layer, borderLayerSuffix);
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
