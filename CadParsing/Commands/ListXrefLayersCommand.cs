using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace CadParsing.Commands
{
    public class ListXrefLayersCommand
    {
        [CommandMethod("LISTXREFLAYERS")]
        public void ListXrefLayers()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document?.Editor;
            if (editor == null) return;

            Database database = document.Database;

            List<string> dependentLayerDescriptions = CollectDependentLayers(database);
            PrintLayerReport(editor, dependentLayerDescriptions);
        }

        private static List<string> CollectDependentLayers(Database database)
        {
            var layerDescriptions = new List<string>();

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = (LayerTable)transaction.GetObject(
                    database.LayerTableId, OpenMode.ForRead);

                foreach (ObjectId layerId in layerTable)
                {
                    LayerTableRecord layerRecord = (LayerTableRecord)transaction.GetObject(
                        layerId, OpenMode.ForRead);

                    if (layerRecord.IsDependent)
                        layerDescriptions.Add(FormatLayerInfo(layerRecord));
                }

                transaction.Commit();
            }

            return layerDescriptions;
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

        private static void PrintLayerReport(
            Editor editor, List<string> layerDescriptions)
        {
            editor.WriteMessage("\n=====================================================");
            editor.WriteMessage(
                "\n XREF-Dependent Layers \u2014 " + layerDescriptions.Count + " found");
            editor.WriteMessage("\n=====================================================");

            foreach (string description in layerDescriptions)
                editor.WriteMessage("\n" + description);

            editor.WriteMessage("\n=====================================================");
            editor.WriteMessage("\n");
        }
    }
}
