using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using CadParsing.Configuration;
using CadParsing.Helpers;

namespace CadParsing.Commands
{
    public class ExportPdfCommand
    {
        private static readonly string[] StyleSheets = { "acad.ctb", "monochrome.ctb" };
        private static readonly string[] StyleSuffixes = { "_color", "_bw" };

        [CommandMethod("EXPORTPDF")]
        public void ExportPdf()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Editor editor = document?.Editor;
            if (editor == null) return;

            Database database = document.Database;

            List<KeyValuePair<ObjectId, double>> detectedBorders =
                BorderHelper.FindBordersInModelSpace(database);

            if (detectedBorders.Count == 0)
            {
                editor.WriteMessage("\n[ERROR] Cannot export PDF \u2014 no border detected.");
                editor.WriteMessage("\n");
                return;
            }

            string drawingFilePath = database.Filename;
            if (string.IsNullOrEmpty(drawingFilePath))
            {
                editor.WriteMessage("\n[ERROR] Save the drawing before exporting.");
                editor.WriteMessage("\n");
                return;
            }

            string outputDirectory = ResolveOutputDirectory(drawingFilePath);
            Directory.CreateDirectory(outputDirectory);

            string drawingSubDirectory = Path.Combine(
                outputDirectory, Path.GetFileNameWithoutExtension(drawingFilePath));

            editor.WriteMessage("\n[INFO] Output directory: " + outputDirectory);
            editor.WriteMessage(string.Format(
                "\n[INFO] {0} border(s) found. Exporting {1} PDFs (colour + B/W)...",
                detectedBorders.Count, detectedBorders.Count * StyleSheets.Length));

            object previousBackgroundPlot = Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", (short)0);

            try
            {
                ExportAllBorders(document, database, editor,
                    detectedBorders, drawingSubDirectory);
            }
            catch (System.Exception exception)
            {
                editor.WriteMessage(
                    "\n[ERROR] PDF export failed: " + exception.Message);
            }
            finally
            {
                Application.SetSystemVariable("BACKGROUNDPLOT", previousBackgroundPlot);
            }

            editor.WriteMessage("\n");
        }

        private static void ExportAllBorders(
            Document document, Database database, Editor editor,
            List<KeyValuePair<ObjectId, double>> detectedBorders,
            string drawingSubDirectory)
        {
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace =
                    DatabaseHelper.GetModelSpaceBlock(transaction, database);
                Layout modelLayout = (Layout)transaction.GetObject(
                    modelSpace.LayoutId, OpenMode.ForRead);

                int totalFileCount = detectedBorders.Count * StyleSheets.Length;
                int currentFileNumber = 0;

                for (int borderIndex = 0; borderIndex < detectedBorders.Count; borderIndex++)
                {
                    Entity borderEntity = transaction.GetObject(
                        detectedBorders[borderIndex].Key, OpenMode.ForRead) as Entity;
                    Extents3d borderExtents = borderEntity.GeometricExtents;

                    string borderLabel = ResolveBorderLabel(
                        editor, transaction, database, borderExtents, borderIndex);

                    if (string.IsNullOrEmpty(borderLabel))
                    {
                        editor.WriteMessage(string.Format(
                            "\n[ERROR] Skipping border {0}: floor plan name not resolved.",
                            borderIndex + 1));
                        continue;
                    }

                    Directory.CreateDirectory(drawingSubDirectory);

                    ExportBorderWithAllStyles(
                        document, editor, modelSpace, modelLayout,
                        borderExtents, drawingSubDirectory, borderLabel,
                        borderIndex, detectedBorders.Count,
                        totalFileCount, ref currentFileNumber);
                }

                transaction.Commit();
            }
        }

        private static string ResolveBorderLabel(
            Editor editor, Transaction transaction, Database database,
            Extents3d borderExtents, int borderIndex)
        {
            string floorPlanName = TextHelper.FindFloorPlanNameInModelSpace(
                transaction, database, borderExtents);

            if (string.IsNullOrEmpty(floorPlanName))
            {
                editor.WriteMessage(string.Format(
                    "\n[ERROR] Floor plan name not found for border {0}."
                    + " Check TEX layer entities and height={1}.",
                    borderIndex + 1, ConfigLoader.Instance.FloorPlanTextHeight));
                return null;
            }

            editor.WriteMessage(string.Format("\n  FloorPlanName: {0}", floorPlanName));
            return SanitizeFileName(floorPlanName);
        }

        private static void ExportBorderWithAllStyles(
            Document document, Editor editor,
            BlockTableRecord modelSpace, Layout modelLayout,
            Extents3d borderExtents, string drawingSubDirectory,
            string borderLabel, int borderIndex, int totalBorders,
            int totalFileCount, ref int currentFileNumber)
        {
            Point3d minPoint = borderExtents.MinPoint;
            Point3d maxPoint = borderExtents.MaxPoint;

            for (int styleIndex = 0; styleIndex < StyleSheets.Length; styleIndex++)
            {
                currentFileNumber++;

                string pdfFilePath = Path.Combine(drawingSubDirectory,
                    string.Format("{0}{1}.pdf",
                        borderLabel, StyleSuffixes[styleIndex]));

                PrintExportHeader(editor, borderIndex, totalBorders,
                    StyleSheets[styleIndex], currentFileNumber,
                    totalFileCount, minPoint, maxPoint, pdfFilePath);

                ExportSinglePdf(document, editor, modelSpace, modelLayout,
                    minPoint, maxPoint, StyleSheets[styleIndex],
                    pdfFilePath, borderIndex, currentFileNumber, totalFileCount);
            }
        }

        private static void PrintExportHeader(
            Editor editor, int borderIndex, int totalBorders,
            string styleSheet, int currentFileNumber, int totalFileCount,
            Point3d minPoint, Point3d maxPoint, string pdfFilePath)
        {
            editor.WriteMessage(string.Format(
                "\n\n--- Border {0} of {1} [{2}] ({3}/{4}) ---",
                borderIndex + 1, totalBorders, styleSheet,
                currentFileNumber, totalFileCount));
            editor.WriteMessage(string.Format(
                "\n  Border: ({0:F2}, {1:F2}) to ({2:F2}, {3:F2})",
                minPoint.X, minPoint.Y, maxPoint.X, maxPoint.Y));
            editor.WriteMessage("\n  Output: " + pdfFilePath);
        }

        private static void ExportSinglePdf(
            Document document, Editor editor,
            BlockTableRecord modelSpace, Layout modelLayout,
            Point3d minPoint, Point3d maxPoint, string styleSheet,
            string pdfFilePath, int borderIndex,
            int currentFileNumber, int totalFileCount)
        {
            PlotInfo plotInfo = ConfigurePlotSettings(
                modelSpace, modelLayout, minPoint, maxPoint, styleSheet);

            PrintDiagnostics(editor, plotInfo.OverrideSettings as PlotSettings);

            if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
            {
                editor.WriteMessage("\n[ERROR] Another plot is already in progress.");
                return;
            }

            try
            {
                PlotToFile(document, plotInfo, pdfFilePath,
                    currentFileNumber, totalFileCount);

                if (File.Exists(pdfFilePath))
                    editor.WriteMessage("\n[INFO] PDF exported: " + pdfFilePath);
                else
                    editor.WriteMessage(
                        "\n[ERROR] Plot completed but no PDF file found at: " + pdfFilePath);
            }
            catch (System.Exception exception)
            {
                editor.WriteMessage(string.Format(
                    "\n[ERROR] PDF export failed for border {0} ({1}): {2}",
                    borderIndex + 1, styleSheet, exception.Message));
            }
        }

        private static PlotInfo ConfigurePlotSettings(
            BlockTableRecord modelSpace, Layout modelLayout,
            Point3d minPoint, Point3d maxPoint, string styleSheet)
        {
            PlotSettings plotSettings = new PlotSettings(modelLayout.ModelType);
            plotSettings.CopyFrom(modelLayout);

            PlotSettingsValidator validator = PlotSettingsValidator.Current;
            validator.RefreshLists(plotSettings);
            validator.SetPlotConfigurationName(plotSettings,
                "AutoCAD PDF (General Documentation).pc3",
                "ISO_A3_(420.00_x_297.00_MM)");
            validator.SetPlotType(plotSettings,
                Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
            validator.SetPlotWindowArea(plotSettings, new Extents2d(
                new Point2d(minPoint.X, minPoint.Y),
                new Point2d(maxPoint.X, maxPoint.Y)));
            validator.SetUseStandardScale(plotSettings, true);
            validator.SetStdScaleType(plotSettings, StdScaleType.ScaleToFit);
            validator.SetCurrentStyleSheet(plotSettings, styleSheet);
            validator.SetPlotRotation(plotSettings, PlotRotation.Degrees000);
            validator.SetPlotCentered(plotSettings, true);
            plotSettings.PrintLineweights = false;

            PlotInfo plotInfo = new PlotInfo();
            plotInfo.Layout = modelSpace.LayoutId;
            plotInfo.OverrideSettings = plotSettings;

            PlotInfoValidator plotInfoValidator = new PlotInfoValidator();
            plotInfoValidator.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
            plotInfoValidator.Validate(plotInfo);

            return plotInfo;
        }

        private static void PrintDiagnostics(Editor editor, PlotSettings plotSettings)
        {
            editor.WriteMessage("\n  [DIAG] Device    : " + plotSettings.PlotConfigurationName);
            editor.WriteMessage("\n  [DIAG] Media     : " + plotSettings.CanonicalMediaName);
            editor.WriteMessage("\n  [DIAG] PlotType  : " + plotSettings.PlotType);
            editor.WriteMessage("\n  [DIAG] StyleSheet: " + plotSettings.CurrentStyleSheet);
            editor.WriteMessage("\n  [DIAG] Scale     : " + plotSettings.StdScaleType);
            editor.WriteMessage("\n  [DIAG] Rotation  : " + plotSettings.PlotRotation);
        }

        private static void PlotToFile(
            Document document, PlotInfo plotInfo,
            string pdfFilePath, int currentFileNumber, int totalFileCount)
        {
            using (PlotEngine plotEngine = PlotFactory.CreatePublishEngine())
            using (PlotProgressDialog progressDialog =
                new PlotProgressDialog(false, 1, true))
            {
                InitializeProgressDialog(
                    progressDialog, currentFileNumber, totalFileCount);

                plotEngine.BeginPlot(progressDialog, null);
                plotEngine.BeginDocument(
                    plotInfo, document.Name, null, 1, true, pdfFilePath);

                PlotSinglePage(plotEngine, progressDialog, plotInfo);

                plotEngine.EndDocument(null);
                plotEngine.EndPlot(null);

                progressDialog.PlotProgressPos = 100;
                progressDialog.OnEndPlot();
            }
        }

        private static void InitializeProgressDialog(
            PlotProgressDialog progressDialog,
            int currentFileNumber, int totalFileCount)
        {
            progressDialog.set_PlotMsgString(PlotMessageIndex.DialogTitle,
                string.Format("EXPORTPDF {0}/{1}", currentFileNumber, totalFileCount));
            progressDialog.set_PlotMsgString(
                PlotMessageIndex.CancelJobButtonMessage, "Cancel");
            progressDialog.set_PlotMsgString(
                PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
            progressDialog.set_PlotMsgString(
                PlotMessageIndex.SheetSetProgressCaption, "Progress");
            progressDialog.set_PlotMsgString(
                PlotMessageIndex.SheetProgressCaption, "Page");

            progressDialog.LowerPlotProgressRange = 0;
            progressDialog.UpperPlotProgressRange = 100;
            progressDialog.PlotProgressPos = 0;

            progressDialog.OnBeginPlot();
            progressDialog.IsVisible = false;
        }

        private static void PlotSinglePage(
            PlotEngine plotEngine, PlotProgressDialog progressDialog,
            PlotInfo plotInfo)
        {
            progressDialog.OnBeginSheet();
            progressDialog.LowerSheetProgressRange = 0;
            progressDialog.UpperSheetProgressRange = 100;
            progressDialog.SheetProgressPos = 0;

            PlotPageInfo pageInfo = new PlotPageInfo();
            plotEngine.BeginPage(pageInfo, plotInfo, true, null);
            plotEngine.BeginGenerateGraphics(null);
            plotEngine.EndGenerateGraphics(null);
            plotEngine.EndPage(null);

            progressDialog.SheetProgressPos = 100;
            progressDialog.OnEndSheet();
        }

        private static string ResolveOutputDirectory(string drawingFilePath)
        {
            AppConfig config = ConfigLoader.Instance;
            string drawingDirectory = Path.GetDirectoryName(drawingFilePath);

            if (drawingDirectory.StartsWith(
                config.DownloadRoot, StringComparison.OrdinalIgnoreCase))
            {
                string relativeDirectory = drawingDirectory
                    .Substring(config.DownloadRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return Path.Combine(config.ExportRoot, relativeDirectory);
            }

            return drawingDirectory;
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');
            return fileName;
        }
    }
}
