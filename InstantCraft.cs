// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Instant Craft", "Strobez", "1.0.0")]
    internal class InstantCraft : RustPlugin
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

        [HarmonyPatch(typeof(ItemCrafter), nameof(ItemCrafter.GetScaledDuration))]
        internal static class ItemCrafter_CraftItem_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(ref float __result)
            {
                __result = 0f;
                return false;
            }
        }

        [HarmonyPatch(typeof(MixingTable), "StartMixing")]
        internal class MixingTable_StartMixing_Patch
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codeInstructionList = new List<CodeInstruction>(instructions);
                int index = codeInstructionList.FindIndex((Predicate<CodeInstruction>) (instruction =>
                    instruction.opcode == OpCodes.Call &&
                    ((MemberInfo) instruction.operand).Name == "set_RemainingMixTime"));
                if (index == -1)
                    return codeInstructionList;

                codeInstructionList[index - 1].opcode = OpCodes.Ldc_R4;
                codeInstructionList[index - 1].operand = 0.0f;
                codeInstructionList.RemoveRange(index - 7, 6);

                return codeInstructionList;
            }
        }
    }
}