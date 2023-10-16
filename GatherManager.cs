// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Gather Manager", "Strobez", "1.0.0")]
    internal class GatherManager : RustPlugin
    {
        private static HarmonyInstance? _harmonyInstance;
        private static const int GATHER_RATE = 10;

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

        [HarmonyPatch(typeof(ResourceDispenser), "GiveResourceFromItem")]
        internal static class ResourceDispenser_GiveResourceFromItem_Patch
        {
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);

                int idx = list.FindLastIndex(x => x.opcode == OpCodes.Ldloc_2);

                if (idx == -1)
                {
                    Debug.LogError("[GatherManager - GiveResourceFromItem_Patch] Failed to find Ldloc_2");
                    return list;
                }

                list.InsertRange(idx + 2, new List<CodeInstruction>
                {
                    new(OpCodes.Ldc_I4, GATHER_RATE),
                    new(OpCodes.Mul)
                });

                return list;
            }
        }

        [HarmonyPatch(typeof(ResourceDispenser), nameof(ResourceDispenser.AssignFinishBonus))]
        internal static class ResourceDispenser_AssignFinishBonus_Patch
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);

                int idx = list.FindLastIndex(x => x.opcode == OpCodes.Ldloc_1);

                if (idx == -1)
                {
                    Debug.LogError("[GatherManager - AssignFinishBonus_Patch] Failed to find Ldloc_1");
                    return list;
                }

                list.InsertRange(idx + 3, new List<CodeInstruction>
                {
                    new(OpCodes.Ldc_I4, GATHER_RATE),
                    new(OpCodes.Mul)
                });

                return list;
            }
        }

        [HarmonyPatch(typeof(CollectibleEntity), nameof(CollectibleEntity.DoPickup))]
        internal static class CollectibleEntity_DoPickup_Patch
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);

                int idx = list.FindLastIndex(x => x.opcode == OpCodes.Ldloc_3);

                if (idx == -1)
                {
                    Debug.LogError("[GatherManager - DoPickup_Patch] Failed to find Ldloc_3");
                    return list;
                }

                list.InsertRange(idx + 3, new List<CodeInstruction>
                {
                    new(OpCodes.Ldc_I4, GATHER_RATE),
                    new(OpCodes.Mul)
                });

                return list;
            }
        }

        [HarmonyPatch(typeof(MiningQuarry), nameof(MiningQuarry.ProcessResources))]
        internal static class MiningQuarry_ProcessResources_Patch
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);

                int idx = list.FindLastIndex(x => x.opcode == OpCodes.Ldloc_2);

                if (idx == -1)
                {
                    Debug.LogError("[GatherManager - ProcessResources_Patch] Failed to find Ldloc_2");
                    return list;
                }

                list.InsertRange(idx + 3, new List<CodeInstruction>
                {
                    new(OpCodes.Ldc_I4, GATHER_RATE),
                    new(OpCodes.Mul)
                });

                return list;
            }
        }
    }
}