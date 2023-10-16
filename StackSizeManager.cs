// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("StackSizeManager", "Strobez", "1.0.0")]
    internal class StackSizeManager : RustPlugin
    {
        #region Harmony
        private static HarmonyInstance? _harmonyInstance;

        private void OnServerInitialized()
        {
            try
            {
                _harmonyInstance = HarmonyInstance.Create(Name + "Patches");
                _harmonyInstance.PatchAll();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating Harmony instance: {ex}");
            }
        }

        private void Unload()
        {
            try
            {
                _harmonyInstance?.UnpatchAll(_harmonyInstance.Id);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error unpatching Harmony instance: {ex}");
            }
        }

        [HarmonyPatch(typeof(Bootstrap), "StartupShared")]
        private static class Bootstrap_StartupShared_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix()
            {
                foreach (ItemDefinition itemDef in ItemManager.itemList)
                {
                    int stackable = itemDef.stackable;

                    if (config == null)
                    {
                        Debug.LogError("Configuration is null");
                        break;
                    }

                    if (config.InvididualStackSizes.TryGetValue(itemDef.shortname, out int individualStackSize))
                        itemDef.stackable = individualStackSize;

                    if (config.IndividualItemStackMultipliers.TryGetValue(itemDef.shortname, out int multiplier))
                        itemDef.stackable = Mathf.RoundToInt(stackable * multiplier);

                    if (config.CategoryStackMultipliers.TryGetValue(itemDef.category.ToString(), out int categoryMultiplier) && categoryMultiplier > 1.0f)
                        itemDef.stackable = Mathf.RoundToInt(stackable * categoryMultiplier);
                }

                return true;
            }

        }
        #endregion

        #region Configuration
        private static Configuration? config;

        public class Configuration
        {

            [JsonProperty("Individial Stack Sizes")]
            public Dictionary<string, int> InvididualStackSizes = new();
            
            [JsonProperty("Invdividual Item Stack Multipliers")]
            public Dictionary<string, int> IndividualItemStackMultipliers = new();
            
            [JsonProperty("Category Stack Multipliers")]
            public Dictionary<string, int> CategoryStackMultipliers = new();

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }

            public Dictionary<string, object> ToDictionary()
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new JsonException();

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }
        #endregion Configuration
    }
}