// Reference: 0Harmony

using System;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Radiation Restriction", "Strobez", "1.0.0")]
    internal class RadiationRestriction : RustPlugin
    {
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

        [HarmonyPatch(typeof(TriggerRadiation), nameof(TriggerRadiation.GetRadiationAmount))]
        private static class TriggerRadiation_GetRadiationAmount_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ref float __result, TriggerRadiation __instance)
            {
                if (__instance.RadiationAmountOverride > 0.0)
                {
                    __result = __instance.RadiationAmountOverride;
                }
                else if (__instance.radiationTier == TriggerRadiation.RadiationTier.HIGH)
                {
                    __result = 51f;
                }

                return false;
            }
        }
    }
}