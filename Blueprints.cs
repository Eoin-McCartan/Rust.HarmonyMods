// Reference: 0Harmony

using System;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Blueprints", "Strobez", "1.0.0")]
    internal class Blueprints : RustPlugin
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

        [HarmonyPatch(typeof(BasePlayer), nameof(BasePlayer.PlayerInit))]
        internal static class BasePlayer_PlayerInit_Patch
        {
            internal static readonly MethodInfo UnlockAll = typeof(PlayerBlueprints).GetMethod(nameof(UnlockAll),
                                                                BindingFlags.Instance |
                                                                BindingFlags.Public |
                                                                BindingFlags.NonPublic) ??
                                                            throw new MissingMethodException(nameof(PlayerBlueprints),
                                                                nameof(UnlockAll));

            [HarmonyPostfix]
            internal static void Postfix(BasePlayer __instance)
            {
                UnlockAll.Invoke(__instance.blueprints, null);
            }
        }
    }
}