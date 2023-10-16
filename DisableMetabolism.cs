// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Disable Metabolism", "Strobez", "1.0.0")]
    internal class DisableMetabolism : RustPlugin
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
        
        [HarmonyPatch(typeof(BasePlayer), nameof(BasePlayer.RespawnAt))]
        internal static class BasePlayer_RespawnAt_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(ref BasePlayer __instance)
            {
                __instance.metabolism.hydration.startMin = 250f;
                __instance.metabolism.hydration.startMax = 250f;
                __instance.metabolism.calories.startMin = 500f;
                __instance.metabolism.calories.startMax = 500f;
                return true;
            }
        }
        
        [HarmonyPatch(typeof(BasePlayer), nameof(BasePlayer.StartHealth))]
        internal static class BasePlayer_StartHealth_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ref float __result)
            {
                __result = 100f;
                return false;
            }
        }
    }
}