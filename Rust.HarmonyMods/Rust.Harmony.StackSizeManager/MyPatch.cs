using Harmony;
using UnityEngine;

namespace Rust.Harmony.StackSizeManager
{
    [HarmonyPatch(typeof(Bootstrap), "StartupShared")]
    internal static class Bootstrap_StartupShared_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix()
        {
            Debug.Log("[Harmony] Loaded: StackSizeManager by Strobez");

            foreach (ItemDefinition itemDef in ItemManager.itemList)
            {
                int stackable = itemDef.stackable;

                if (HarmonyConfig.Config == null)
                {
                    Debug.LogError("Configuration is null");
                    break;
                }

                if (HarmonyConfig.Config.InvididualStackSizes.TryGetValue(itemDef.shortname, out int individualStackSize))
                    itemDef.stackable = individualStackSize;

                if (HarmonyConfig.Config.IndividualItemStackMultipliers.TryGetValue(itemDef.shortname, out int multiplier))
                    itemDef.stackable = Mathf.RoundToInt(stackable * multiplier);

                if (HarmonyConfig.Config.CategoryStackMultipliers.TryGetValue(itemDef.category.ToString(),
                        out int categoryMultiplier) &&
                    categoryMultiplier > 1.0f)
                    itemDef.stackable = Mathf.RoundToInt(stackable * categoryMultiplier);
            }

            return true;
        }
    }
}