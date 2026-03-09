using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace CadParsing.Helpers
{
    internal static class TextFontOverride
    {
        public static IReadOnlyList<ObjectId> FindTextEntitiesOnTargetLayers(
            Transaction transaction, Database database, string[] targetSuffixes)
        {
            var result = new List<ObjectId>();
            BlockTableRecord modelSpace = DatabaseHelper.GetModelSpaceBlock(transaction, database);

            foreach (ObjectId objectId in modelSpace)
            {
                try
                {
                    Entity entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
                    if (entity == null) continue;

                    if (!(entity is DBText || entity is MText)) continue;

                    if (!LayerNameMatcher.MatchesAnyLayerSuffix(entity.Layer, targetSuffixes))
                        continue;

                    result.Add(objectId);
                }
                catch (Exception)
                {
                    // Skip entities that cannot be opened
                }
            }

            return result;
        }

        public static Dictionary<ObjectId, ObjectId> ApplyStandardFontOverride(
            Transaction transaction,
            IReadOnlyList<ObjectId> entityIds,
            Database database,
            Editor editor)
        {
            var savedStyles = new Dictionary<ObjectId, ObjectId>();
            ObjectId standardStyleId = ResolveOrCreateStandardStyle(transaction, database, editor);

            foreach (ObjectId entityId in entityIds)
            {
                try
                {
                    Entity entity = (Entity)transaction.GetObject(entityId, OpenMode.ForWrite);

                    if (entity is DBText dbText)
                    {
                        savedStyles[entityId] = dbText.TextStyleId;
                        dbText.TextStyleId = standardStyleId;
                    }
                    else if (entity is MText mText)
                    {
                        savedStyles[entityId] = mText.TextStyleId;
                        mText.TextStyleId = standardStyleId;
                    }
                }
                catch (Exception exception)
                {
                    editor.WriteMessage(string.Format(
                        "\n[WARN] TextFontOverride: Cannot override font for entity {0}: {1}",
                        entityId, exception.Message));
                }
            }

            return savedStyles;
        }

        public static void RestoreOriginalTextStyles(
            Transaction transaction,
            Dictionary<ObjectId, ObjectId> savedTextStyles,
            Editor editor)
        {
            if (savedTextStyles == null || savedTextStyles.Count == 0)
                return;

            foreach (KeyValuePair<ObjectId, ObjectId> entry in savedTextStyles)
            {
                try
                {
                    Entity entity = (Entity)transaction.GetObject(entry.Key, OpenMode.ForWrite);

                    if (entity is DBText dbText)
                        dbText.TextStyleId = entry.Value;
                    else if (entity is MText mText)
                        mText.TextStyleId = entry.Value;
                }
                catch (Exception exception)
                {
                    editor.WriteMessage(string.Format(
                        "\n[WARN] TextFontOverride: Cannot restore font for entity {0}: {1}",
                        entry.Key, exception.Message));
                }
            }
        }

        private static ObjectId ResolveOrCreateStandardStyle(
            Transaction transaction, Database database, Editor editor)
        {
            TextStyleTable textStyleTable = (TextStyleTable)transaction.GetObject(
                database.TextStyleTableId, OpenMode.ForRead);

            if (textStyleTable.Has("Standard"))
                return textStyleTable["Standard"];

            // Create Standard style as fallback for malformed drawings
            textStyleTable.UpgradeOpen();
            TextStyleTableRecord newStyle = new TextStyleTableRecord();
            newStyle.Name = "Standard";
            newStyle.FileName = "txt.shx";
            ObjectId newStyleId = textStyleTable.Add(newStyle);
            transaction.AddNewlyCreatedDBObject(newStyle, true);
            editor.WriteMessage("\n[INFO] TextFontOverride: Created 'Standard' text style.");
            return newStyleId;
        }
    }
}
