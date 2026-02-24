using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(CadParsing.CadParsingApp))]
[assembly: CommandClass(typeof(CadParsing.CadParsingApp))]

namespace CadParsing
{
    public class CadParsingApp : IExtensionApplication
    {
        public void Initialize()
        {
            Console.WriteLine("CadParsing plugin initialized.");
        }

        public void Terminate() { }

        [CommandMethod("LISTXREFLAYERS")]
        public void ListXrefLayers()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc?.Editor;
            if (ed == null) return;

            Database db = doc.Database;
            List<string> xrefLayers = new List<string>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                foreach (ObjectId layerId in lt)
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);

                    if (ltr.IsDependent)
                    {
                        string info = string.Format(
                            "  {0,-50} | Color: {1,-12} | IsFrozen: {2,-5} | IsOff: {3}",
                            ltr.Name,
                            ltr.Color.ColorNameForDisplay,
                            ltr.IsFrozen,
                            ltr.IsOff);

                        xrefLayers.Add(info);
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\n=====================================================");
            ed.WriteMessage("\n XREF-Dependent Layers — " + xrefLayers.Count + " found");
            ed.WriteMessage("\n=====================================================");

            foreach (string layer in xrefLayers)
                ed.WriteMessage("\n" + layer);

            ed.WriteMessage("\n=====================================================");
            ed.WriteMessage("\n");
        }

        [CommandMethod("BINDXREF")]
        public void BindXref()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc?.Editor;
            if (ed == null) return;

            Database db = doc.Database;
            ObjectIdCollection xrefIds = new ObjectIdCollection();

            // --- Step 1 & 2: Iterate BlockTable, collect resolved Xref ObjectIds ---
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                    foreach (ObjectId btrId in bt)
                    {
                        BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                        if (btr == null || !btr.IsFromExternalReference) continue;

                        if (btr.XrefStatus != XrefStatus.Resolved)
                        {
                            ed.WriteMessage(string.Format(
                                "\n  [SKIP] {0} — Status: {1}", btr.Name, btr.XrefStatus));
                            continue;
                        }

                        ed.WriteMessage("\n  [XREF] Queued for bind: " + btr.Name);
                        xrefIds.Add(btrId);
                    }

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("\n[ERROR] Failed to collect Xref blocks: " + ex.Message);
                    return;
                }
            }

            if (xrefIds.Count == 0)
            {
                ed.WriteMessage("\n[WARN] No resolved Xref blocks found to bind.");
                ed.WriteMessage("\n");
                return;
            }

            ed.WriteMessage("\n  " + xrefIds.Count + " Xref(s) queued.");

            // --- Step 3 & 4: Bind Xrefs — BindXrefs manages its own internal transaction.
            // Wrap in try/catch so any failure is reported without leaving the DB in a
            // half-modified state (BindXrefs rolls itself back on error internally). ---
            try
            {
                db.BindXrefs(xrefIds, true);
                ed.WriteMessage("\n[INFO] BindXrefs completed successfully (Insert mode).");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[ERROR] BindXrefs failed: " + ex.Message);
                return;
            }

            // --- Step 5: List all layers after binding ---
            ed.WriteMessage("\n=====================================================");
            ed.WriteMessage("\n Layers after Xref bind:");
            ed.WriteMessage("\n=====================================================");

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    int layerCount = 0;

                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);

                        string info = string.Format(
                            "  {0,-50} | Color: {1,-12} | IsFrozen: {2,-5} | IsOff: {3}",
                            ltr.Name,
                            ltr.Color.ColorNameForDisplay,
                            ltr.IsFrozen,
                            ltr.IsOff);

                        ed.WriteMessage("\n" + info);
                        layerCount++;
                    }

                    ed.WriteMessage("\n=====================================================");
                    ed.WriteMessage("\n Total: " + layerCount + " layer(s)");
                    ed.WriteMessage("\n=====================================================");

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("\n[ERROR] Failed to list layers: " + ex.Message);
                }
            }

            ed.WriteMessage("\n");
        }

        [CommandMethod("EXPLODEBLOCK")]
        public void ExplodeBlock()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc?.Editor;
            if (ed == null) return;

            Database db = doc.Database;
            int explodedCount = 0;
            int skippedCount = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // --- Build locked-layer lookup so we can guard every write operation ---
                HashSet<string> lockedLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId layerId in lt)
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    if (ltr.IsLocked)
                        lockedLayers.Add(ltr.Name);
                }

                // --- Collect target spaces: Model Space + all Paper Space layouts ---
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                List<ObjectId> targetSpaces = new List<ObjectId>();
                targetSpaces.Add(bt[BlockTableRecord.ModelSpace]);

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                    if (btr != null && btr.IsLayout && btrId != bt[BlockTableRecord.ModelSpace])
                        targetSpaces.Add(btrId);
                }

                // --- Process each space ---
                foreach (ObjectId spaceId in targetSpaces)
                {
                    BlockTableRecord space =
                        (BlockTableRecord)tr.GetObject(spaceId, OpenMode.ForWrite);

                    // Snapshot IDs before touching the collection to avoid mid-iteration issues
                    List<ObjectId> entityIds = new List<ObjectId>();
                    foreach (ObjectId entId in space)
                        entityIds.Add(entId);

                    foreach (ObjectId entId in entityIds)
                    {
                        if (entId.IsErased) continue;

                        Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        BlockReference br = ent as BlockReference;
                        if (br == null) continue;

                        // Step 7: Guard — skip BlockReferences that live on a locked layer
                        if (lockedLayers.Contains(br.Layer))
                        {
                            ed.WriteMessage("\n  [SKIP] BlockReference on locked layer: " + br.Layer);
                            skippedCount++;
                            continue;
                        }

                        // Steps 3 & 4: Recurse until only primitive entities remain
                        DBObjectCollection primitives = new DBObjectCollection();
                        try
                        {
                            ExplodeRecursive(br, primitives, lockedLayers, ed);
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage("\n  [ERROR] Explode failed (handle " +
                                br.Handle + "): " + ex.Message);
                            foreach (DBObject o in primitives) o.Dispose();
                            continue;
                        }

                        // Step 5: Add all resulting primitives to the space
                        foreach (DBObject obj in primitives)
                        {
                            Entity primitive = obj as Entity;
                            if (primitive == null) { obj.Dispose(); continue; }
                            space.AppendEntity(primitive);
                            tr.AddNewlyCreatedDBObject(primitive, true);
                        }

                        // Step 6: Erase the original BlockReference now that contents are safely added
                        br.UpgradeOpen();
                        br.Erase();
                        explodedCount++;
                    }
                }

                // Step 8: Commit
                tr.Commit();
            }

            ed.WriteMessage(string.Format(
                "\n[INFO] EXPLODEBLOCK: {0} block(s) fully exploded, {1} skipped (locked layer).",
                explodedCount, skippedCount));
            ed.WriteMessage("\n");
        }

        [CommandMethod("DETECTBORDER")]
        public void DetectBorder()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc?.Editor;
            if (ed == null) return;

            Database db = doc.Database;

            List<KeyValuePair<ObjectId, double>> borders = FindBorders(db, ed);
            if (borders.Count == 0)
            {
                ed.WriteMessage("\n[WARN] No border detected on target layer.");
                ed.WriteMessage("\n");
                return;
            }

            ed.WriteMessage(string.Format(
                "\n[INFO] {0} border(s) detected (by area).", borders.Count));

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int idx = 0; idx < borders.Count; idx++)
                {
                    ObjectId borderId = borders[idx].Key;
                    double bboxArea = borders[idx].Value;

                    Entity ent = tr.GetObject(borderId, OpenMode.ForRead) as Entity;
                    Extents3d ext = ent.GeometricExtents;

                    ed.WriteMessage("\n=====================================================");
                    ed.WriteMessage(string.Format("\n BORDER {0} of {1}", idx + 1, borders.Count));
                    ed.WriteMessage("\n=====================================================");
                    ed.WriteMessage("\n  Handle    : " + ent.Handle);
                    ed.WriteMessage("\n  Layer     : " + ent.Layer);
                    ed.WriteMessage("\n  Type      : " + ent.GetRXClass().Name);
                    ed.WriteMessage(string.Format("\n  BBox Area : {0:F4}", bboxArea));
                    ed.WriteMessage(string.Format(
                        "\n  Min       : ({0:F4},  {1:F4})", ext.MinPoint.X, ext.MinPoint.Y));
                    ed.WriteMessage(string.Format(
                        "\n  Max       : ({0:F4},  {1:F4})", ext.MaxPoint.X, ext.MaxPoint.Y));
                    ed.WriteMessage("\n-----------------------------------------------------");

                    if (ent is Polyline lwp)
                    {
                        Matrix3d ocsToWcs = Matrix3d.WorldToPlane(lwp.Normal).Inverse();

                        ed.WriteMessage(string.Format(
                            "\n  Vertices  : {0}  |  Normal({1:F4}, {2:F4}, {3:F4})  |  Elevation: {4:F4}",
                            lwp.NumberOfVertices,
                            lwp.Normal.X, lwp.Normal.Y, lwp.Normal.Z,
                            lwp.Elevation));
                        ed.WriteMessage("\n-----------------------------------------------------");

                        for (int i = 0; i < lwp.NumberOfVertices; i++)
                        {
                            Point2d ocs = lwp.GetPoint2dAt(i);
                            Point3d wcs = new Point3d(ocs.X, ocs.Y, lwp.Elevation)
                                              .TransformBy(ocsToWcs);

                            ed.WriteMessage(string.Format(
                                "\n  [{0,3}]  OCS({1,12:F4}, {2,12:F4})" +
                                "  WCS({3,12:F4}, {4,12:F4}, {5,8:F4})",
                                i, ocs.X, ocs.Y, wcs.X, wcs.Y, wcs.Z));
                        }
                    }
                    else if (ent is Polyline2d p2d)
                    {
                        Matrix3d ocsToWcs = Matrix3d.WorldToPlane(p2d.Normal).Inverse();

                        ed.WriteMessage(string.Format(
                            "\n  Vertices (Polyline2d)  |  Normal({0:F4}, {1:F4}, {2:F4})",
                            p2d.Normal.X, p2d.Normal.Y, p2d.Normal.Z));
                        ed.WriteMessage("\n-----------------------------------------------------");

                        int i = 0;
                        foreach (ObjectId vId in p2d)
                        {
                            Vertex2d v = tr.GetObject(vId, OpenMode.ForRead) as Vertex2d;
                            if (v == null) continue;

                            Point3d wcs = v.Position.TransformBy(ocsToWcs);

                            ed.WriteMessage(string.Format(
                                "\n  [{0,3}]  OCS({1,12:F4}, {2,12:F4})" +
                                "  WCS({3,12:F4}, {4,12:F4}, {5,8:F4})",
                                i++, v.Position.X, v.Position.Y, wcs.X, wcs.Y, wcs.Z));
                        }
                    }

                    ed.WriteMessage("\n=====================================================");
                }

                tr.Commit();
            }

            ed.WriteMessage("\n");
        }

        [CommandMethod("EXPORTPDF")]
        public void ExportPdf()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc?.Editor;
            if (ed == null) return;

            Database db = doc.Database;

            // --- Get all borders on target layer ---
            List<KeyValuePair<ObjectId, double>> borders = FindBorders(db, ed);
            if (borders.Count == 0)
            {
                ed.WriteMessage("\n[ERROR] Cannot export PDF \u2014 no border detected.");
                ed.WriteMessage("\n");
                return;
            }

            // Derive base PDF path from the DWG filename
            string dwgPath = db.Filename;
            if (string.IsNullOrEmpty(dwgPath))
            {
                ed.WriteMessage("\n[ERROR] Save the drawing before exporting.");
                ed.WriteMessage("\n");
                return;
            }

            string baseName = Path.Combine(
                Path.GetDirectoryName(dwgPath),
                Path.GetFileNameWithoutExtension(dwgPath));

            ed.WriteMessage(string.Format(
                "\n[INFO] {0} border(s) found. Exporting {1} PDFs (colour + B/W)...",
                borders.Count, borders.Count * 2));

            // Force foreground plotting
            object bgPlotOld = Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", (short)0);

            // Style table entries: acad.ctb for colour, monochrome.ctb for B/W
            string[] styleSheets = new string[] { "acad.ctb", "monochrome.ctb" };
            string[] styleSuffixes = new string[] { "_color", "_bw" };

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord msBlock = (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                    Layout msLayout = (Layout)tr.GetObject(msBlock.LayoutId, OpenMode.ForRead);

                    int totalCount = borders.Count * styleSheets.Length;
                    int fileNum = 0;

                    for (int idx = 0; idx < borders.Count; idx++)
                    {
                        ObjectId borderId = borders[idx].Key;
                        Entity borderEnt = tr.GetObject(borderId, OpenMode.ForRead) as Entity;
                        Extents3d borderExtents = borderEnt.GeometricExtents;

                        Point3d minPt = borderExtents.MinPoint;
                        Point3d maxPt = borderExtents.MaxPoint;

                        for (int s = 0; s < styleSheets.Length; s++)
                        {
                            fileNum++;
                            string pdfPath = string.Format("{0}_{1}{2}.pdf",
                                baseName, idx + 1, styleSuffixes[s]);

                            ed.WriteMessage(string.Format(
                                "\n\n--- Border {0} of {1} [{2}] ({3}/{4}) ---",
                                idx + 1, borders.Count, styleSheets[s], fileNum, totalCount));
                            ed.WriteMessage(string.Format(
                                "\n  Border: ({0:F2}, {1:F2}) to ({2:F2}, {3:F2})",
                                minPt.X, minPt.Y, maxPt.X, maxPt.Y));
                            ed.WriteMessage("\n  Output: " + pdfPath);

                            // --- Define PlotSettings ---
                            PlotSettings ps = new PlotSettings(msLayout.ModelType);
                            ps.CopyFrom(msLayout);

                            PlotSettingsValidator psv = PlotSettingsValidator.Current;
                            psv.RefreshLists(ps);

                            psv.SetPlotConfigurationName(ps,
                                "AutoCAD PDF (General Documentation).pc3",
                                "ISO_A3_(420.00_x_297.00_MM)");

                            psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                            psv.SetPlotWindowArea(ps, new Extents2d(
                                new Point2d(minPt.X, minPt.Y),
                                new Point2d(maxPt.X, maxPt.Y)));

                            psv.SetUseStandardScale(ps, true);
                            psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
                            psv.SetCurrentStyleSheet(ps, styleSheets[s]);
                            psv.SetPlotRotation(ps, PlotRotation.Degrees000);
                            psv.SetPlotCentered(ps, true);

                            PlotInfo pi = new PlotInfo();
                            pi.Layout = msBlock.LayoutId;
                            pi.OverrideSettings = ps;

                            PlotInfoValidator piv = new PlotInfoValidator();
                            piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                            piv.Validate(pi);

                            ed.WriteMessage("\n  [DIAG] Device    : " + ps.PlotConfigurationName);
                            ed.WriteMessage("\n  [DIAG] Media     : " + ps.CanonicalMediaName);
                            ed.WriteMessage("\n  [DIAG] PlotType  : " + ps.PlotType);
                            ed.WriteMessage("\n  [DIAG] StyleSheet: " + ps.CurrentStyleSheet);
                            ed.WriteMessage("\n  [DIAG] Scale     : " + ps.StdScaleType);
                            ed.WriteMessage("\n  [DIAG] Rotation  : " + ps.PlotRotation);

                            if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
                            {
                                ed.WriteMessage("\n[ERROR] Another plot is already in progress.");
                                continue;
                            }

                            try
                            {
                                using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                                using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                                {
                                    ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle,
                                        string.Format("EXPORTPDF {0}/{1}", fileNum, totalCount));
                                    ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel");
                                    ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                                    ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Progress");
                                    ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Page");

                                    ppd.LowerPlotProgressRange = 0;
                                    ppd.UpperPlotProgressRange = 100;
                                    ppd.PlotProgressPos = 0;

                                    ppd.OnBeginPlot();
                                    ppd.IsVisible = false;

                                    pe.BeginPlot(ppd, null);
                                    pe.BeginDocument(pi, doc.Name, null, 1, true, pdfPath);

                                    ppd.OnBeginSheet();
                                    ppd.LowerSheetProgressRange = 0;
                                    ppd.UpperSheetProgressRange = 100;
                                    ppd.SheetProgressPos = 0;

                                    PlotPageInfo ppi = new PlotPageInfo();
                                    pe.BeginPage(ppi, pi, true, null);
                                    pe.BeginGenerateGraphics(null);
                                    pe.EndGenerateGraphics(null);
                                    pe.EndPage(null);

                                    ppd.SheetProgressPos = 100;
                                    ppd.OnEndSheet();

                                    pe.EndDocument(null);
                                    pe.EndPlot(null);

                                    ppd.PlotProgressPos = 100;
                                    ppd.OnEndPlot();
                                }

                                if (File.Exists(pdfPath))
                                    ed.WriteMessage("\n[INFO] PDF exported: " + pdfPath);
                                else
                                    ed.WriteMessage("\n[ERROR] Plot completed but no PDF file found at: " + pdfPath);
                            }
                            catch (System.Exception ex)
                            {
                                ed.WriteMessage(string.Format(
                                    "\n[ERROR] PDF export failed for border {0} ({1}): {2}",
                                    idx + 1, styleSheets[s], ex.Message));
                            }
                        }
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[ERROR] PDF export failed: " + ex.Message);
            }
            finally
            {
                Application.SetSystemVariable("BACKGROUNDPLOT", bgPlotOld);
            }

            ed.WriteMessage("\n");
        }

        [CommandMethod("HELLOWORLD")]
        public void HelloWorld()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc?.Editor;
            if (ed == null)
            {
                Console.WriteLine("HELLO WORLD");
                return;
            }

            Database db = doc.Database;
            ed.WriteMessage("\nHELLO KELVIN");
            ed.WriteMessage("\n[DWG] " + db.Filename);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                int count = 0;
                foreach (ObjectId _ in ms) count++;

                ed.WriteMessage("\n[DWG] Entity count in Model Space: " + count);
                tr.Commit();
            }

            ed.WriteMessage("\n");
        }
        private bool FindBorder(
                Database db,
                Editor ed,
                out ObjectId borderId,
                out Extents3d borderExtents)
        {
                borderId = ObjectId.Null;
                borderExtents = new Extents3d();

                const string targetLayer = "\uc0ac\uc5c5\uc2b9\uc778\uc2dc\ud2b8$0$PAPER-EX";

                SelectionFilter filter = new SelectionFilter(new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Operator,  "<AND"),
                    new TypedValue((int)DxfCode.Operator,  "<OR"),
                    new TypedValue((int)DxfCode.Start,     "LWPOLYLINE"),
                    new TypedValue((int)DxfCode.Start,     "POLYLINE"),
                    new TypedValue((int)DxfCode.Operator,  "OR>"),
                    new TypedValue((int)DxfCode.LayerName, targetLayer),
                    new TypedValue((int)DxfCode.Operator,  "AND>")
                });

                PromptSelectionResult psr = ed.SelectAll(filter);
                if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
                    return false;

                double largestArea = double.MinValue;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        if (so == null) continue;

                        Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        bool closed = false;
                        if (ent is Polyline lwp)        closed = lwp.Closed;
                        else if (ent is Polyline2d p2d) closed = p2d.Closed;

                        if (!closed) continue;

                        try
                        {
                            Extents3d ext = ent.GeometricExtents;
                            double area = (ext.MaxPoint.X - ext.MinPoint.X)
                                        * (ext.MaxPoint.Y - ext.MinPoint.Y);

                            if (area > largestArea)
                            {
                                largestArea = area;
                                borderId = so.ObjectId;
                            }
                        }
                        catch (System.Exception) { /* no valid extents \u2014 skip */ }
                    }

                    tr.Commit();
                }

                if (borderId.IsNull) return false;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(borderId, OpenMode.ForRead) as Entity;
                    borderExtents = ent.GeometricExtents;
                    tr.Commit();
                }

                return true;
            }

        private List<KeyValuePair<ObjectId, double>> FindBorders(
            Database db,
            Editor ed)
        {
            const string targetLayer = "\uc0ac\uc5c5\uc2b9\uc778\uc2dc\ud2b8$0$PAPER-EX";

            SelectionFilter filter = new SelectionFilter(new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator,  "<AND"),
                new TypedValue((int)DxfCode.Operator,  "<OR"),
                new TypedValue((int)DxfCode.Start,     "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start,     "POLYLINE"),
                new TypedValue((int)DxfCode.Operator,  "OR>"),
                new TypedValue((int)DxfCode.LayerName, targetLayer),
                new TypedValue((int)DxfCode.Operator,  "AND>")
            });

            PromptSelectionResult psr = ed.SelectAll(filter);
            if (psr.Status != PromptStatus.OK || psr.Value == null || psr.Value.Count == 0)
                return new List<KeyValuePair<ObjectId, double>>();

            List<KeyValuePair<ObjectId, double>> candidates =
                new List<KeyValuePair<ObjectId, double>>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null) continue;

                    Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    bool closed = false;
                    if (ent is Polyline lwp)        closed = lwp.Closed;
                    else if (ent is Polyline2d p2d) closed = p2d.Closed;

                    if (!closed) continue;

                    try
                    {
                        Extents3d ext = ent.GeometricExtents;
                        double area = (ext.MaxPoint.X - ext.MinPoint.X)
                                    * (ext.MaxPoint.Y - ext.MinPoint.Y);

                        candidates.Add(new KeyValuePair<ObjectId, double>(so.ObjectId, area));
                    }
                    catch (System.Exception) { /* no valid extents — skip */ }
                }

                tr.Commit();
            }

            candidates.Sort((a, b) => b.Value.CompareTo(a.Value));

            return candidates;
        }

                private static void ExplodeRecursive(
                    Entity ent,
                    DBObjectCollection primitives,
                    HashSet<string> lockedLayers,
                    Editor ed)
            {
                DBObjectCollection exploded = new DBObjectCollection();
                ent.Explode(exploded);

                foreach (DBObject obj in exploded)
                {
                    Entity child = obj as Entity;
                    if (child == null) { obj.Dispose(); continue; }

                    BlockReference nestedBr = child as BlockReference;
                    if (nestedBr != null)
                    {
                        // Step 7: Guard nested blocks on locked layers
                        if (lockedLayers.Contains(nestedBr.Layer))
                        {
                            ed.WriteMessage("\n  [SKIP] Nested block on locked layer: " + nestedBr.Layer);
                            child.Dispose();
                            continue;
                        }

                        // Step 4c & 4d: Still a BlockReference — recurse deeper
                        ExplodeRecursive(nestedBr, primitives, lockedLayers, ed);
                        child.Dispose(); // Intermediate shell is no longer needed
                    }
                    else
                    {
                        // Primitive entity (Line, Arc, Polyline, etc.) — collect it
                        primitives.Add(child);
                    }
                }
            }
        }
    }
