using System.IO;
using CadParsing.Configuration;
using NUnit.Framework;

namespace CadParsing.Tests.Unit
{
    [TestFixture]
    public class ConfigLoaderTests
    {
        [Test]
        public void LoadFromFile_ValidJson_LoadsAllFields()
        {
            string json = "{\"BorderLayerSuffix\":\"MY-BORDER\","
                + "\"TextLayerSuffix\":\"MY-TEX\","
                + "\"FloorPlanTextHeight\":500.0,"
                + "\"TextHeightTolerance\":1.0,"
                + "\"AcceptClosedPolylinesOnly\":false,"
                + "\"DownloadRoot\":\"C:\\\\test\\\\downloads\","
                + "\"ExportRoot\":\"C:\\\\test\\\\export\"}";

            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, json);
                AppConfig config = ConfigLoader.LoadFromFile(tempFile);

                Assert.That(config.BorderLayerSuffix, Is.EqualTo("MY-BORDER"));
                Assert.That(config.TextLayerSuffix, Is.EqualTo("MY-TEX"));
                Assert.That(config.FloorPlanTextHeight, Is.EqualTo(500.0));
                Assert.That(config.TextHeightTolerance, Is.EqualTo(1.0));
                Assert.That(config.AcceptClosedPolylinesOnly, Is.False);
                Assert.That(config.DownloadRoot, Is.EqualTo(@"C:\test\downloads"));
                Assert.That(config.ExportRoot, Is.EqualTo(@"C:\test\export"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void LoadFromFile_MissingFile_ReturnsDefaults()
        {
            AppConfig config = ConfigLoader.LoadFromFile(
                @"C:\nonexistent\path\cadparsing.config.json");

            Assert.That(config.BorderLayerSuffix, Is.EqualTo("PAPER-EX"));
            Assert.That(config.TextLayerSuffix, Is.EqualTo("TEX"));
            Assert.That(config.FloorPlanTextHeight, Is.EqualTo(400.0));
            Assert.That(config.TextHeightTolerance, Is.EqualTo(0.5));
            Assert.That(config.AcceptClosedPolylinesOnly, Is.True);
        }

        [Test]
        public void LoadFromFile_MalformedJson_ReturnsDefaults()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "this is not valid json {{{}");
                AppConfig config = ConfigLoader.LoadFromFile(tempFile);

                Assert.That(config.BorderLayerSuffix, Is.EqualTo("PAPER-EX"));
                Assert.That(config.TextLayerSuffix, Is.EqualTo("TEX"));
                Assert.That(config.FloorPlanTextHeight, Is.EqualTo(400.0));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void LoadFromFile_EmptyBorderLayerSuffix_ReturnsDefaults()
        {
            string json = "{\"BorderLayerSuffix\":\"\",\"TextLayerSuffix\":\"TEX\"}";
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, json);
                AppConfig config = ConfigLoader.LoadFromFile(tempFile);

                Assert.That(config.BorderLayerSuffix, Is.EqualTo("PAPER-EX"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void LoadFromFile_EmptyTextLayerSuffix_ReturnsDefaults()
        {
            string json = "{\"BorderLayerSuffix\":\"PAPER-EX\",\"TextLayerSuffix\":\"\"}";
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, json);
                AppConfig config = ConfigLoader.LoadFromFile(tempFile);

                Assert.That(config.TextLayerSuffix, Is.EqualTo("TEX"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
