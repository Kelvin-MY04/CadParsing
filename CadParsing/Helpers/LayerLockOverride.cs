using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace CadParsing.Helpers
{
    internal static class LayerLockOverride
    {
        /// <summary>
        /// Returns the unique set of locked layer ObjectIds referenced by the given entity list.
        /// Per-entity exceptions are caught silently; the method never throws.
        /// </summary>
        public static ISet<ObjectId> CollectLockedLayerIds(
            Transaction transaction,
            IReadOnlyList<ObjectId> entityIds)
        {
            var lockedLayerIds = new HashSet<ObjectId>();

            foreach (ObjectId entityId in entityIds)
            {
                try
                {
                    Entity entity = transaction.GetObject(entityId, OpenMode.ForRead) as Entity;
                    if (entity == null) continue;

                    LayerTableRecord layerRecord =
                        transaction.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
                    if (layerRecord == null) continue;

                    if (layerRecord.IsLocked)
                        lockedLayerIds.Add(entity.LayerId);
                }
                catch (Exception)
                {
                    // Skip entities that cannot be opened for reading
                }
            }

            return lockedLayerIds;
        }

        /// <summary>
        /// Temporarily unlocks the specified layers, logs one INFO message per layer,
        /// and returns a dictionary of saved lock states for later restoration.
        /// Per-layer exceptions are caught and logged as warnings.
        /// </summary>
        public static Dictionary<ObjectId, bool> UnlockLayers(
            Transaction transaction,
            ISet<ObjectId> lockedLayerIds,
            Editor editor)
        {
            var savedLockStates = new Dictionary<ObjectId, bool>();

            if (lockedLayerIds == null || lockedLayerIds.Count == 0)
                return savedLockStates;

            foreach (ObjectId layerId in lockedLayerIds)
            {
                try
                {
                    LayerTableRecord layerRecord =
                        transaction.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                    if (layerRecord == null) continue;

                    savedLockStates[layerId] = layerRecord.IsLocked;
                    layerRecord.UpgradeOpen();
                    layerRecord.IsLocked = false;

                    editor.WriteMessage(string.Format(
                        "\n[INFO] LayerLockOverride: Temporarily unlocking layer '{0}'.",
                        layerRecord.Name));
                }
                catch (Exception exception)
                {
                    editor.WriteMessage(string.Format(
                        "\n[WARN] LayerLockOverride: Could not unlock layer {0}: {1}",
                        layerId, exception.Message));
                }
            }

            return savedLockStates;
        }

        /// <summary>
        /// Restores each layer's IsLocked property to its saved value.
        /// Guard-returns on null or empty input. Per-layer exceptions are logged as warnings.
        /// </summary>
        public static void RestoreLayerLocks(
            Transaction transaction,
            Dictionary<ObjectId, bool> savedLockStates,
            Editor editor)
        {
            if (savedLockStates == null || savedLockStates.Count == 0)
                return;

            foreach (KeyValuePair<ObjectId, bool> entry in savedLockStates)
            {
                try
                {
                    LayerTableRecord layerRecord =
                        transaction.GetObject(entry.Key, OpenMode.ForWrite) as LayerTableRecord;
                    if (layerRecord == null) continue;

                    layerRecord.IsLocked = entry.Value;
                }
                catch (Exception exception)
                {
                    editor.WriteMessage(string.Format(
                        "\n[WARN] LayerLockOverride: Could not restore lock state for layer {0}: {1}",
                        entry.Key, exception.Message));
                }
            }
        }
    }
}
