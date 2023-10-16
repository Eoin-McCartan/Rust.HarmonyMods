// Reference: 0Harmony

using System;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Disable Cold", "Strobez", "1.0.0")]
    internal class DisableCold : RustPlugin
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
        
        [HarmonyPatch(typeof(BasePlayer), nameof(BasePlayer.ServerInit))]
        internal static class BasePlayer_ServerInit_Patch
        {
            [HarmonyPrefix]
            internal static void Postfix(BasePlayer __instance)
            {
                __instance.metabolism.temperature.max = 20f;
                __instance.metabolism.temperature.min = 20f;
            }
        }
    }
}