using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace CadParsing.Commands
{
    public class HelloWorldCommand
    {
        [CommandMethod("HELLOWORLD")]
        public void HelloWorld()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document?.Editor;
            if (editor == null)
            {
                Console.WriteLine("HELLO WORLD");
                return;
            }

            Database database = document.Database;
            editor.WriteMessage("\nHELLO KELVIN");
            editor.WriteMessage("\n[DWG] " + database.Filename);

            int entityCount = CountModelSpaceEntities(database);
            editor.WriteMessage("\n[DWG] Entity count in Model Space: " + entityCount);
            editor.WriteMessage("\n");
        }

        private static int CountModelSpaceEntities(Database database)
        {
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(
                    database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                int count = 0;
                foreach (ObjectId _ in modelSpace) count++;

                transaction.Commit();
                return count;
            }
        }
    }
}
