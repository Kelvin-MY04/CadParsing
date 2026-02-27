using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace CadParsing.Helpers
{
    internal static class TextColorOverride
    {
        private static readonly Color BlackColor = Color.FromRgb(0, 0, 0);

        public static Dictionary<ObjectId, Color> ApplyBlackOverride(
            Transaction transaction,
            IReadOnlyList<ObjectId> textEntityIds,
            Editor editor)
        {
            var savedColors = new Dictionary<ObjectId, Color>();

            foreach (ObjectId entityId in textEntityIds)
            {
                try
                {
                    Entity entity =
                        (Entity)transaction.GetObject(entityId, OpenMode.ForWrite);
                    savedColors[entityId] = entity.Color;
                    entity.Color = BlackColor;
                }
                catch (Exception exception)
                {
                    editor.WriteMessage(string.Format(
                        "\n[WARN] TextColorOverride: Cannot override color for entity {0}: {1}",
                        entityId, exception.Message));
                }
            }

            return savedColors;
        }

        public static void RestoreOriginalColors(
            Transaction transaction,
            Dictionary<ObjectId, Color> savedColors,
            Editor editor)
        {
            foreach (KeyValuePair<ObjectId, Color> entry in savedColors)
            {
                try
                {
                    Entity entity =
                        (Entity)transaction.GetObject(entry.Key, OpenMode.ForWrite);
                    entity.Color = entry.Value;
                }
                catch (Exception exception)
                {
                    editor.WriteMessage(string.Format(
                        "\n[WARN] TextColorOverride: Cannot restore color for entity {0}: {1}",
                        entry.Key, exception.Message));
                }
            }
        }
    }
}
