using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace CadParsing.Helpers
{
    internal static class ExplodeHelper
    {
        public static void ExplodeRecursive(
            Entity entity,
            DBObjectCollection primitiveEntities,
            HashSet<string> lockedLayerNames,
            Editor editor)
        {
            DBObjectCollection explodedChildren = new DBObjectCollection();
            entity.Explode(explodedChildren);

            foreach (DBObject childObject in explodedChildren)
            {
                Entity childEntity = childObject as Entity;
                if (childEntity == null)
                {
                    childObject.Dispose();
                    continue;
                }

                BlockReference nestedBlock = childEntity as BlockReference;
                if (nestedBlock != null)
                {
                    ProcessNestedBlock(
                        nestedBlock, primitiveEntities, lockedLayerNames, editor);
                }
                else
                {
                    primitiveEntities.Add(childEntity);
                }
            }
        }

        private static void ProcessNestedBlock(
            BlockReference nestedBlock,
            DBObjectCollection primitiveEntities,
            HashSet<string> lockedLayerNames,
            Editor editor)
        {
            if (lockedLayerNames.Contains(nestedBlock.Layer))
            {
                editor.WriteMessage(
                    "\n  [SKIP] Nested block on locked layer: " + nestedBlock.Layer);
                nestedBlock.Dispose();
                return;
            }

            ExplodeRecursive(nestedBlock, primitiveEntities, lockedLayerNames, editor);
            nestedBlock.Dispose();
        }
    }
}
