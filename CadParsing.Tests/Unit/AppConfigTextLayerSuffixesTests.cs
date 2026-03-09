using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using CadParsing.Configuration;
using NUnit.Framework;


namespace CadParsing.Tests.Unit
{
    [TestFixture]
    public class AppConfigTextLayerSuffixesTests
    {
        [Test]
        public void DefaultValue_IsTextAndTex()
        {
            AppConfig config = new AppConfig();

            Assert.That(config.TextLayerSuffixes, Is.EqualTo(new[] { "TEXT", "TEX" }));
        }

        [Test]
        public void Deserialize_CustomArray_LoadsCorrectValues()
        {
            string json = "{\"TextLayerSuffixes\":[\"ANNO\",\"LBL\"]}";

            AppConfig config = DeserializeJson(json);

            Assert.That(config.TextLayerSuffixes, Is.EqualTo(new[] { "ANNO", "LBL" }));
        }

        [Test]
        public void LoadFromFile_MissingTextLayerSuffixesKey_FallsBackToDefault()
        {
            // DataContractJsonSerializer leaves array null when key is absent;
            // ConfigLoader.ValidateOrDefault detects null and returns new AppConfig() with defaults.
            string json = "{\"BorderLayerSuffix\":\"Sheet\"}";
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, json);
                AppConfig config = ConfigLoader.LoadFromFile(tempFile);

                Assert.That(config.TextLayerSuffixes, Is.EqualTo(new[] { "TEXT", "TEX" }));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        private static AppConfig DeserializeJson(string json)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(AppConfig));
                return (AppConfig)serializer.ReadObject(stream);
            }
        }
    }
}
