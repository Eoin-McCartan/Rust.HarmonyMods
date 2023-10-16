// Reference: 0Harmony

using System;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vending Machine Drop", "Strobez", "1.0.0")]
    internal class VendingMachineDrop : RustPlugin
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

        [HarmonyPatch(typeof(VendingMachine), nameof(VendingMachine.ServerInit))]
        private static class VendingMachine_ServerInit_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(VendingMachine __instance)
            {
                __instance.dropsLoot = true;
            
                return true;
            }
        }
    }
}