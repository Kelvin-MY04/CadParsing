using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace CadParsing.Commands
{
    public class BindXrefCommand
    {
        [CommandMethod("BINDXREF")]
        public void BindXref()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document?.Editor;
            if (editor == null) return;

            Database database = document.Database;

            ObjectIdCollection resolvedXrefIds = CollectResolvedXrefs(database, editor);
            if (resolvedXrefIds.Count == 0)
            {
                editor.WriteMessage("\n[WARN] No resolved Xref blocks found to bind.");
                editor.WriteMessage("\n");
                return;
            }

            editor.WriteMessage("\n  " + resolvedXrefIds.Count + " Xref(s) queued.");

            if (!TryBindXrefs(database, resolvedXrefIds, editor))
                return;

            PrintAllLayers(database, editor);
            editor.WriteMessage("\n");
        }

        private static ObjectIdCollection CollectResolvedXrefs(
            Database database, Editor editor)
        {
            ObjectIdCollection xrefIds = new ObjectIdCollection();

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable blockTable = (BlockTable)transaction.GetObject(
                        database.BlockTableId, OpenMode.ForRead);

                    foreach (ObjectId blockId in blockTable)
                    {
                        BlockTableRecord blockRecord = transaction.GetObject(
                            blockId, OpenMode.ForRead) as BlockTableRecord;

                        if (blockRecord == null || !blockRecord.IsFromExternalReference)
                            continue;

                        if (blockRecord.XrefStatus != XrefStatus.Resolved)
                        {
                            editor.WriteMessage(string.Format(
                                "\n  [SKIP] {0} \u2014 Status: {1}",
                                blockRecord.Name, blockRecord.XrefStatus));
                            continue;
                        }

                        editor.WriteMessage(
                            "\n  [XREF] Queued for bind: " + blockRecord.Name);
                        xrefIds.Add(blockId);
                    }

                    transaction.Commit();
                }
                catch (System.Exception exception)
                {
                    editor.WriteMessage(
                        "\n[ERROR] Failed to collect Xref blocks: " + exception.Message);
                }
            }

            return xrefIds;
        }

        private static bool TryBindXrefs(
            Database database, ObjectIdCollection xrefIds, Editor editor)
        {
            try
            {
                database.BindXrefs(xrefIds, true);
                editor.WriteMessage(
                    "\n[INFO] BindXrefs completed successfully (Insert mode).");
                return true;
            }
            catch (System.Exception exception)
            {
                editor.WriteMessage(
                    "\n[ERROR] BindXrefs failed: " + exception.Message);
                return false;
            }
        }

        private static void PrintAllLayers(Database database, Editor editor)
        {
            editor.WriteMessage("\n=====================================================");
            editor.WriteMessage("\n Layers after Xref bind:");
            editor.WriteMessage("\n=====================================================");

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = (LayerTable)transaction.GetObject(
                        database.LayerTableId, OpenMode.ForRead);
                    int layerCount = 0;

                    foreach (ObjectId layerId in layerTable)
                    {
                        LayerTableRecord layerRecord = (LayerTableRecord)transaction.GetObject(
                            layerId, OpenMode.ForRead);

                        editor.WriteMessage("\n" + FormatLayerInfo(layerRecord));
                        layerCount++;
                    }

                    editor.WriteMessage("\n=====================================================");
                    editor.WriteMessage("\n Total: " + layerCount + " layer(s)");
                    editor.WriteMessage("\n=====================================================");

                    transaction.Commit();
                }
                catch (System.Exception exception)
                {
                    editor.WriteMessage(
                        "\n[ERROR] Failed to list layers: " + exception.Message);
                }
            }
        }

        private static string FormatLayerInfo(LayerTableRecord layerRecord)
        {
            return string.Format(
                "  {0,-50} | Color: {1,-12} | IsFrozen: {2,-5} | IsOff: {3}",
                layerRecord.Name,
                layerRecord.Color.ColorNameForDisplay,
                layerRecord.IsFrozen,
                layerRecord.IsOff);
        }
    }
}
