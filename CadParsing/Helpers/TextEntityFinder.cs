using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace CadParsing.Helpers
{
    internal static class TextEntityFinder
    {
        public static IReadOnlyList<ObjectId> FindAllTextEntities(
            Transaction transaction, Database database)
        {
            BlockTableRecord modelSpace =
                DatabaseHelper.GetModelSpaceBlock(transaction, database);

            var foundEntityIds = new List<ObjectId>();
            var visitedBlockDefinitionIds = new HashSet<ObjectId>();

            CollectTextEntitiesFromBlock(
                transaction, modelSpace, foundEntityIds, visitedBlockDefinitionIds);

            return foundEntityIds.AsReadOnly();
        }

        private static void CollectTextEntitiesFromBlock(
            Transaction transaction,
            BlockTableRecord blockTableRecord,
            List<ObjectId> foundEntityIds,
            HashSet<ObjectId> visitedBlockDefinitionIds)
        {
            if (!visitedBlockDefinitionIds.Add(blockTableRecord.ObjectId))
                return;

            foreach (ObjectId entityId in blockTableRecord)
            {
                Entity entity = TryOpenEntity(transaction, entityId);
                if (entity == null) continue;

                if (IsTextBearingEntity(entity))
                {
                    foundEntityIds.Add(entityId);
                }
                else if (entity is BlockReference blockReference)
                {
                    BlockTableRecord nestedDefinition = TryOpenBlockDefinition(
                        transaction, blockReference.BlockTableRecord);

                    if (nestedDefinition != null)
                        CollectTextEntitiesFromBlock(
                            transaction, nestedDefinition,
                            foundEntityIds, visitedBlockDefinitionIds);
                }
            }
        }

        private static bool IsTextBearingEntity(Entity entity)
        {
            return entity is DBText
                || entity is MText
                || entity is Dimension
                || entity is Leader
                || entity is MLeader;
        }

        private static Entity TryOpenEntity(Transaction transaction, ObjectId entityId)
        {
            try
            {
                return transaction.GetObject(entityId, OpenMode.ForRead) as Entity;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static BlockTableRecord TryOpenBlockDefinition(
            Transaction transaction, ObjectId blockDefinitionId)
        {
            try
            {
                return transaction.GetObject(
                    blockDefinitionId, OpenMode.ForRead) as BlockTableRecord;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
