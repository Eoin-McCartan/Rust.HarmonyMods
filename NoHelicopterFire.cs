// Reference: 0Harmony

using System;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("No Helicopter Fire", "Strobez", "1.0.0")]
    internal class NoHelicopterFire : RustPlugin
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

        [HarmonyPatch(typeof(BaseHelicopter), nameof(BaseHelicopter.ServerInit))]
        private static class BaseHelicopter_ServerInit_Patch
        {
            [HarmonyPostfix]
            internal static void Postfix(BaseHelicopter __instance)
            {
                __instance.fireBall.guid = null;
                __instance.serverGibs.guid = null;
            }
        }
    }
}