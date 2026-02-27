using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CadParsing.Configuration
{
    public static class ConfigLoader
    {
        private static AppConfig _instance;

        public static AppConfig Instance => _instance ?? (_instance = LoadFromAssemblyDirectory());

        private static AppConfig LoadFromAssemblyDirectory()
        {
            string directory = Path.GetDirectoryName(typeof(ConfigLoader).Assembly.Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(directory, "cadparsing.config.json");
            return LoadFromFile(configPath);
        }

        public static AppConfig LoadFromFile(string configFilePath)
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    Console.WriteLine(
                        "[WARN] ConfigLoader: Config file not found at " + configFilePath
                        + ". Using defaults.");
                    return new AppConfig();
                }

                string jsonContent = File.ReadAllText(configFilePath, Encoding.UTF8);
                return DeserializeOrDefault(jsonContent, configFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[WARN] ConfigLoader: Failed to read config file: " + ex.Message
                    + ". Using defaults.");
                return new AppConfig();
            }
        }

        private static AppConfig DeserializeOrDefault(string jsonContent, string configFilePath)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent)))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(AppConfig));
                    AppConfig config = (AppConfig)serializer.ReadObject(stream);
                    return ValidateOrDefault(config, configFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[WARN] ConfigLoader: Failed to parse config file: " + ex.Message
                    + ". Using defaults.");
                return new AppConfig();
            }
        }

        private static AppConfig ValidateOrDefault(AppConfig config, string configFilePath)
        {
            if (string.IsNullOrEmpty(config.BorderLayerSuffix))
            {
                Console.WriteLine(
                    "[WARN] ConfigLoader: BorderLayerSuffix is empty in " + configFilePath
                    + ". Using defaults.");
                return new AppConfig();
            }

            if (string.IsNullOrEmpty(config.TextLayerSuffix))
            {
                Console.WriteLine(
                    "[WARN] ConfigLoader: TextLayerSuffix is empty in " + configFilePath
                    + ". Using defaults.");
                return new AppConfig();
            }

            return config;
        }
    }
}
