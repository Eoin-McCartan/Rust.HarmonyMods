// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Disable Life Story", "Strobez", "1.0.0")]
    internal class DisableLifeStory : RustPlugin
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
        
        [HarmonyPatch(typeof(BasePlayer), "LifeStoryStart")]
        internal static class BasePlayer_LifeStoryStart_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(BasePlayer __instance)
            {
                return false;
            }
        }
    }
}