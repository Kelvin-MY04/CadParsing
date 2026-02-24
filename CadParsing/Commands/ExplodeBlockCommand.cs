using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CadParsing.Helpers;

namespace CadParsing.Commands
{
    public class ExplodeBlockCommand
    {
        [CommandMethod("EXPLODEBLOCK")]
        public void ExplodeBlock()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document?.Editor;
            if (editor == null) return;

            Database database = document.Database;
            int explodedCount = 0;
            int skippedCount = 0;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                HashSet<string> lockedLayerNames = CollectLockedLayerNames(transaction, database);
                List<ObjectId> layoutSpaceIds = CollectAllLayoutSpaces(transaction, database);

                foreach (ObjectId spaceId in layoutSpaceIds)
                {
                    ExplodeTargetBlocksInSpace(
                        transaction, spaceId, lockedLayerNames, editor,
                        ref explodedCount, ref skippedCount);
                }

                transaction.Commit();
            }

            editor.WriteMessage(string.Format(
                "\n[INFO] EXPLODEBLOCK: {0} block(s) ending with [{2}] fully exploded, {1} skipped (locked layer).",
                explodedCount, skippedCount, Constants.BlockSuffix));
            editor.WriteMessage("\n");
        }

        private static HashSet<string> CollectLockedLayerNames(
            Transaction transaction, Database database)
        {
            var lockedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LayerTable layerTable = (LayerTable)transaction.GetObject(
                database.LayerTableId, OpenMode.ForRead);

            foreach (ObjectId layerId in layerTable)
            {
                LayerTableRecord layerRecord = (LayerTableRecord)transaction.GetObject(
                    layerId, OpenMode.ForRead);
                if (layerRecord.IsLocked)
                    lockedNames.Add(layerRecord.Name);
            }

            return lockedNames;
        }

        private static List<ObjectId> CollectAllLayoutSpaces(
            Transaction transaction, Database database)
        {
            BlockTable blockTable = (BlockTable)transaction.GetObject(
                database.BlockTableId, OpenMode.ForRead);

            ObjectId modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
            var spaceIds = new List<ObjectId> { modelSpaceId };

            foreach (ObjectId blockId in blockTable)
            {
                BlockTableRecord blockRecord = transaction.GetObject(
                    blockId, OpenMode.ForRead) as BlockTableRecord;
                if (blockRecord != null && blockRecord.IsLayout && blockId != modelSpaceId)
                    spaceIds.Add(blockId);
            }

            return spaceIds;
        }

        private static void ExplodeTargetBlocksInSpace(
            Transaction transaction,
            ObjectId spaceId,
            HashSet<string> lockedLayerNames,
            Editor editor,
            ref int explodedCount,
            ref int skippedCount)
        {
            BlockTableRecord space = (BlockTableRecord)transaction.GetObject(
                spaceId, OpenMode.ForWrite);

            List<ObjectId> entityIds = SnapshotEntityIds(space);

            foreach (ObjectId entityId in entityIds)
            {
                if (entityId.IsErased) continue;

                BlockReference blockRef = transaction.GetObject(
                    entityId, OpenMode.ForRead) as BlockReference;
                if (blockRef == null) continue;

                if (!IsTargetBlock(blockRef))
                    continue;

                if (lockedLayerNames.Contains(blockRef.Layer))
                {
                    editor.WriteMessage(
                        "\n  [SKIP] BlockReference on locked layer: " + blockRef.Layer);
                    skippedCount++;
                    continue;
                }

                if (TryExplodeAndReplace(transaction, space, blockRef, lockedLayerNames, editor))
                    explodedCount++;
            }
        }

        private static List<ObjectId> SnapshotEntityIds(BlockTableRecord space)
        {
            var ids = new List<ObjectId>();
            foreach (ObjectId entityId in space)
                ids.Add(entityId);
            return ids;
        }

        private static bool IsTargetBlock(BlockReference blockRef)
        {
            return blockRef.Name.EndsWith(
                Constants.BlockSuffix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryExplodeAndReplace(
            Transaction transaction,
            BlockTableRecord space,
            BlockReference blockRef,
            HashSet<string> lockedLayerNames,
            Editor editor)
        {
            DBObjectCollection primitiveEntities = new DBObjectCollection();
            try
            {
                ExplodeHelper.ExplodeRecursive(
                    blockRef, primitiveEntities, lockedLayerNames, editor);
            }
            catch (System.Exception exception)
            {
                editor.WriteMessage(string.Format(
                    "\n  [ERROR] Explode failed (handle {0}): {1}",
                    blockRef.Handle, exception.Message));
                DisposePrimitives(primitiveEntities);
                return false;
            }

            AddPrimitivesToSpace(transaction, space, primitiveEntities);
            EraseOriginalBlock(blockRef);
            return true;
        }

        private static void AddPrimitivesToSpace(
            Transaction transaction, BlockTableRecord space,
            DBObjectCollection primitiveEntities)
        {
            foreach (DBObject dbObject in primitiveEntities)
            {
                Entity primitiveEntity = dbObject as Entity;
                if (primitiveEntity == null) { dbObject.Dispose(); continue; }

                space.AppendEntity(primitiveEntity);
                transaction.AddNewlyCreatedDBObject(primitiveEntity, true);
            }
        }

        private static void EraseOriginalBlock(BlockReference blockRef)
        {
            blockRef.UpgradeOpen();
            blockRef.Erase();
        }

        private static void DisposePrimitives(DBObjectCollection primitives)
        {
            foreach (DBObject dbObject in primitives)
                dbObject.Dispose();
        }
    }
}
