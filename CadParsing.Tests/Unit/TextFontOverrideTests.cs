using NUnit.Framework;

namespace CadParsing.Tests.Unit
{
    // TextFontOverride lives in the CadParsing project, which references the AutoCAD SDK.
    // CadParsing.Tests references only CadParsing.Core (no AutoCAD SDK dependency),
    // so direct unit tests for TextFontOverride cannot be compiled here.
    //
    // Guard-clause and round-trip testing for TextFontOverride is validated manually:
    //   1. Run EXPORTPDF on a drawing with missing fonts.
    //   2. Confirm the exported PDF shows readable text (no '????' placeholders). [US1]
    //   3. Compare the DWG file's last-modified timestamp before and after export;
    //      it must be unchanged. [US2]
    //
    // The 'finally' block in ExportPdfCommand.ExportAllBorders guarantees
    // RestoreOriginalTextStyles executes even when a border export throws.
    [TestFixture]
    [Ignore("TextFontOverride requires the AutoCAD SDK; validate manually via EXPORTPDF command.")]
    public class TextFontOverrideTests
    {
        [Test]
        public void RestoreOriginalTextStyles_EmptyDictionary_DoesNotThrow()
        {
            // Requires live AutoCAD transaction — validated manually.
        }

        [Test]
        public void RestoreOriginalTextStyles_NullDictionary_DoesNotThrow()
        {
            // Requires live AutoCAD transaction — validated manually.
        }
    }
}
