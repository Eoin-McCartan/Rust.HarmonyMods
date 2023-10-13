using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Rust.Harmony.StackSizeManager
{
    public class HarmonyConfig
    {
        public static ConfigData? Config;
        private static readonly string Location = Path.Combine(nameof(HarmonyConfig), "StackSizeManager.json");

        public static void LoadConfig()
        {
            if (!Directory.Exists(nameof(HarmonyConfig)))
                Directory.CreateDirectory(nameof(HarmonyConfig));
            if (!File.Exists(Location))
                LoadDefaultConfig();
            else
                try
                {
                    Config = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(Location));
                }
                catch
                {
                    LoadDefaultConfig();
                }
        }

        private static void LoadDefaultConfig()
        {
            Config = new ConfigData();
            File.WriteAllText(Location, JsonConvert.SerializeObject(Config));
        }

        public static void SaveConfig()
        {
            File.WriteAllText(Location, JsonConvert.SerializeObject(Config));
        }

        public class ConfigData
        {

            [JsonProperty("Category Stack Multipliers")]
            public Dictionary<string, int> CategoryStackMultipliers = new();

            [JsonProperty("Invdividual Item Stack Multipliers")]
            public Dictionary<string, int> IndividualItemStackMultipliers = new();

            [JsonProperty("Individial Stack Sizes")]
            public Dictionary<string, int> InvididualStackSizes = new();

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }

            public Dictionary<string, object> ToDictionary()
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
            }
        }
    }
}