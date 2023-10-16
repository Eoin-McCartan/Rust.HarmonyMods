// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vending In Stock", "Strobez", "1.0.0")]
    internal class VendingInStock : RustPlugin
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

        [HarmonyPatch(typeof(VendingMachine), nameof(VendingMachine.BuyItem))]
        private static class VendingMachine_BuyItem_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ref BaseEntity.RPCMessage rpc)
            {
                if (!rpc.player.inventory.containerMain.IsFull() || !rpc.player.inventory.containerBelt.IsFull())
                    return true;

                rpc.player.ShowToast(GameTip.Styles.Red_Normal, (Translate.Phrase) "Inventory Full");

                return false;
            }

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo completePendingOrder = AccessTools.Method(typeof(VendingMachine), "CompletePendingOrder");

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.opcode == OpCodes.Call && (instruction.operand as MemberInfo)?.Name == "SetPendingOrder")
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Callvirt, completePendingOrder);
                        yield return new CodeInstruction(OpCodes.Ret);
                        yield break;
                    }
                }
            }
        }
    }
}