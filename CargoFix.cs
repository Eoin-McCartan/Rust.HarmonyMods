// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Blueprints", "Strobez", "1.0.0")]
    internal class CargoFix : RustPlugin
    {
        private const int TotalCrates = 3;
        private const float StartEgressAfterAllCratesHackedSeconds = 60f;
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

        [HarmonyPatch(typeof(CargoShip), nameof(CargoShip.Spawn))]
        internal static class CargoShip_Spawn_Patch
        {
        }

        [HarmonyPatch(typeof(CargoShip), nameof(CargoShip.RespawnLoot))]
        internal static class CargoShip_RespawnLoot_Patch
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codeInstructions = new List<CodeInstruction>(instructions);

                int idx = codeInstructions.FindLastIndex(x => x.opcode == OpCodes.Ldsfld &&
                                                              x.operand is FieldInfo
                                                              {
                                                                  Name: "loot_rounds"
                                                              });

                if (idx == -1)
                {
                    Debug.LogError("[CargoFix - CargoShip_RespawnLoot_Patch] Failed to find Ldsfld");
                    return codeInstructions;
                }

                codeInstructions[idx] = new CodeInstruction(OpCodes.Ldc_I4_1);

                return codeInstructions;
            }
        }
    }
}