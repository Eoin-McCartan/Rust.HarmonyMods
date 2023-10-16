// Reference: 0Harmony

using System;
using System.Linq;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Insta Barrel", "Strobez", "1.0.0")]
    internal class InstaBarrel : RustPlugin
    {
        private static HarmonyInstance? _harmonyInstance;

        private static string[] _lootContainers =
        {
            "barrel",
            "roadsign"
        };

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

        [HarmonyPatch(typeof(LootContainer), nameof(LootContainer.OnAttacked), typeof(HitInfo))]
        internal static class LootContainer_OnAttacked_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(HitInfo info, LootContainer __instance)
            {
                try
                {
                    
                    if (info is null || __instance is null)
                        return true;
                    
                    if (!(info.InitiatorPlayer is { } player))
                        return true;
                    
                    foreach (string lootContainer in _lootContainers)
                    {
                        if (!__instance.ShortPrefabName.Contains(lootContainer))
                            continue;
                        
                        if (!(__instance is StorageContainer storageContainer) || Vector3.Distance(player.transform.position, storageContainer.transform.position) > 20f)
                            return true;
                        
                        for (int i = storageContainer.inventory.itemList.Count; i > 0; --i)
                            player.GiveItem(storageContainer.inventory.itemList[i - 1], BaseEntity.GiveItemReason.PickedUp);
                        
                        info.damageTypes.ScaleAll(999f);
                        break;
                    }
                }
                catch (Exception)
                {
                    // Handle the exception or ignore it
                }

                return true;
            }
        }
    }
}