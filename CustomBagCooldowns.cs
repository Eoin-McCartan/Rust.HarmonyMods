// Reference: 0Harmony

using System;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Bag Cooldowns", "Strobez", "1.0.0")]
    internal class CustomBagCooldowns : RustPlugin
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

        [HarmonyPatch(typeof(SleepingBag), nameof(SleepingBag.ServerInit))]
        internal static class SleepingBag_ServerInit_Patch
        {
            [HarmonyPrefix]
            internal static void Postfix(SleepingBag __instance)
            {
                __instance.secondsBetweenReuses = 0.5f;
                __instance.SetUnlockTime(__instance.unlockSeconds * 0.5f + Time.realtimeSinceStartup);
            }
        }
    }
}