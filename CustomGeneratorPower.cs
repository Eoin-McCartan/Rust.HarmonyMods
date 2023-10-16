// Reference: 0Harmony

using System;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Generator Power", "Strobez", "1.0.0")]
    internal class CustomGeneratorPower : RustPlugin
    {
        private static HarmonyInstance? _harmonyInstance;
        private const float _electricAmount = 100f;
        
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
        
        [HarmonyPatch(typeof(ElectricGenerator), nameof(ElectricGenerator.GetCurrentEnergy))]
        internal static class ElectricGenerator_GetCurrentEnergy_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(ref int __result, ElectricGenerator __instance)
            {
                if (__instance.OwnerID == 0L)
                    return true;
                
                __result = (int) _electricAmount;

                return false;
            }
        }
        
        [HarmonyPatch(typeof(ElectricGenerator), nameof(ElectricGenerator.MaximalPowerOutput))]
        internal static class ElectricGenerator_MaximalPowerOutput_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(ref int __result, ElectricGenerator __instance)
            {
                if (__instance.OwnerID == 0L)
                    return true;
                
                __result = Mathf.FloorToInt(_electricAmount);
                
                return false;
            }
        }
    }
}