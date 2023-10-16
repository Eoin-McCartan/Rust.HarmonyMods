// Reference: 0Harmony

using System;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Conveyor Transfer Rate", "Strobez", "1.0.0")]
    internal class ConveyorTransferRate  : RustPlugin
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

        [HarmonyPatch(typeof(IndustrialConveyor), nameof(IndustrialConveyor.ServerInit))]
        internal static class IndustrialConveyor_ServerInit_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(IndustrialConveyor __instance)
            {
                __instance.MaxStackSizePerMove = 128; // default is 128
                ConVar.Server.conveyorMoveFrequency = 25f; // default is 5f

                return true;
            }
        }
    }
}