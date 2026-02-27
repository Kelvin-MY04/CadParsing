using Autodesk.AutoCAD.DatabaseServices;

namespace CadParsing.Helpers
{
    internal static class DatabaseHelper
    {
        public static BlockTableRecord GetModelSpaceBlock(
            Transaction transaction, Database database)
        {
            BlockTable blockTable = (BlockTable)transaction.GetObject(
                database.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        }
    }
}
