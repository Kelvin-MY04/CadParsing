using System.IO;
using CadParsing.Helpers;
using NUnit.Framework;

namespace CadParsing.Tests.Unit
{
    [TestFixture]
    public class ExportPathBuilderTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // T003 [US1]
        [Test]
        public void BuildPdfPath_GivenColorStyle_ReturnsColorSubfolderPath()
        {
            string result = ExportPathBuilder.BuildPdfPath("C:/out/Bldg", "Color-PDF", "Level 1");
            Assert.That(result, Is.EqualTo(Path.Combine("C:/out/Bldg", "Color-PDF", "Level 1.pdf")));
        }

        // T004 [US1]
        [Test]
        public void BuildPdfPath_GivenBwStyle_ReturnsBwSubfolderPath()
        {
            string result = ExportPathBuilder.BuildPdfPath("C:/out/Bldg", "BW-PDF", "Level 1");
            Assert.That(result, Is.EqualTo(Path.Combine("C:/out/Bldg", "BW-PDF", "Level 1.pdf")));
        }

        // T005 [US1]
        [Test]
        public void CreateTypeSubFolders_CreatesBothSubfolders()
        {
            ExportPathBuilder.CreateTypeSubFolders(_tempDir);

            Assert.That(Directory.Exists(Path.Combine(_tempDir, "Color-PDF")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(_tempDir, "BW-PDF")), Is.True);
        }

        // T006 [US1]
        [Test]
        public void CreateTypeSubFolders_IsIdempotent_WhenFoldersAlreadyExist()
        {
            ExportPathBuilder.CreateTypeSubFolders(_tempDir);

            Assert.DoesNotThrow(() => ExportPathBuilder.CreateTypeSubFolders(_tempDir));
            Assert.That(Directory.Exists(Path.Combine(_tempDir, "Color-PDF")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(_tempDir, "BW-PDF")), Is.True);
        }

        // T012 [US2]
        [Test]
        public void BuildPdfPath_FilenameContainsNoColorSuffix()
        {
            string result = ExportPathBuilder.BuildPdfPath("C:/out/Bldg", "Color-PDF", "Level 1");
            string nameWithoutExt = Path.GetFileNameWithoutExtension(result);
            Assert.That(nameWithoutExt, Does.Not.Contain("_color"));
        }

        // T013 [US2]
        [Test]
        public void BuildPdfPath_FilenameContainsNoBwSuffix()
        {
            string result = ExportPathBuilder.BuildPdfPath("C:/out/Bldg", "BW-PDF", "Level 1");
            string nameWithoutExt = Path.GetFileNameWithoutExtension(result);
            Assert.That(nameWithoutExt, Does.Not.Contain("_bw"));
        }
    }
}
