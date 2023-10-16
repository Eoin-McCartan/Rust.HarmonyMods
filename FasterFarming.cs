// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Faster Farming", "Strobez", "1.0.0")]
    internal class FasterFarming : RustPlugin
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

        [HarmonyPatch(typeof(OreResourceEntity), nameof(OreResourceEntity.OnAttacked), typeof(HitInfo))]
        internal static class OreResourceEntity_OnAttacked_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(HitInfo info, OreResourceEntity __instance)
            {
                if (info is not null && __instance is not null)
                {
                    if (__instance._hotSpot == null)
                        __instance._hotSpot = __instance.SpawnBonusSpot(info.HitPositionWorld);
            
                    __instance._hotSpot.transform.position = info.HitPositionWorld;
                    __instance._hotSpot?.SendNetworkUpdateImmediate();
                }
            
                return true;
            }
        }
        
        [HarmonyPatch(typeof(TreeEntity), nameof(TreeEntity.DidHitMarker), typeof(HitInfo))]
        internal static class TreeEntity_DidHitMarker_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(HitInfo info, ref bool __result)
            {
                if (info is null)
                    return true;
                
                __result = true;
                
                return false;
            }
        }
    }
}