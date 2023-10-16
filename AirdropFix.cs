// Reference: 0Harmony

using System;
using Harmony;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Oxide.Plugins
{
    [Info("AirdropFix", "Strobez", "1.0.0")]
    internal class AirdropFix : RustPlugin
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

        [HarmonyPatch(typeof(TimedExplosive), nameof(TimedExplosive.SetFuse))]
        internal static class TimedExplosive_SetFuse_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(TimedExplosive __instance)
            {
                if (__instance is SupplySignal supplySignal)
                {
                    Vector3 spawnPoint = __instance.transform.position;

                    __instance.Invoke(() =>
                    {
                        spawnPoint.y = TerrainMeta.HighestPoint.y;
                        BaseEntity entity =
                            GameManager.server.CreateEntity("assets/prefabs/misc/supply drop/supply_drop.prefab",
                                spawnPoint);

                        if ((bool) (Object) entity)
                        {
                            entity.globalBroadcast = true;
                            entity.Spawn();
                            entity.GetComponent<Rigidbody>().drag = 0.5f;
                        }
                    }, 15f);

                    __instance.Invoke(supplySignal.FinishUp, 210f);
                    __instance.SendNetworkUpdateImmediate();

                    return false;
                }

                return true;
            }
        }
    }
}