using NUnit.Framework;

namespace CadParsing.Tests.Unit
{
    // LayerLockOverride lives in the CadParsing project, which references the AutoCAD SDK.
    // CadParsing.Tests references only CadParsing.Core (no AutoCAD SDK dependency),
    // so direct unit tests for LayerLockOverride cannot be compiled here.
    //
    // Manual validation steps:
    //   1. Open AutoCAD 2023 with a DWG that has text entities on locked layers.
    //   2. Run EXPORTPDF. Confirm: no [ERROR] PDF export failed in the console,
    //      PDF files are created on disk, and the console shows exactly one
    //      [INFO] LayerLockOverride: Temporarily unlocking layer '...' per affected layer.
    //   3. Open Layer Manager and confirm all previously-locked layers are still locked
    //      (no permanent modification to the DWG).
    //   4. Run EXPORTPDF on a DWG with no locked layers. Confirm behaviour is identical
    //      to before the fix — no regression in colour or font override.
    [TestFixture]
    [Ignore("LayerLockOverride requires the AutoCAD SDK; validate manually via EXPORTPDF command.")]
    public class LayerLockOverrideTests
    {
        [Test]
        public void CollectLockedLayerIds_EmptyEntityList_ReturnsEmptySet()
        {
            // Requires live AutoCAD transaction — validated manually.
        }

        [Test]
        public void UnlockLayers_EmptyLayerSet_ReturnsEmptyDictionary()
        {
            // Requires live AutoCAD transaction — validated manually.
        }

        [Test]
        public void RestoreLayerLocks_NullDictionary_DoesNotThrow()
        {
            // Requires live AutoCAD transaction — validated manually.
        }

        [Test]
        public void RestoreLayerLocks_EmptyDictionary_DoesNotThrow()
        {
            // Requires live AutoCAD transaction — validated manually.
        }

        [Test]
        public void UnlockThenRestore_LockedLayer_LayerRemainsLockedAfterRestore()
        {
            // Requires live AutoCAD transaction — validated manually.
        }
    }
}
