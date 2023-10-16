// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Recycler Speed", "Strobez", "1.0.0")]
    internal class RecyclerSpeed : RustPlugin
    {
        private static HarmonyInstance? _harmonyInstance;
        private const float _recyclerSpeed = 0.5f;

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

        [HarmonyPatch(typeof(Recycler), nameof(Recycler.StartRecycling))]
        internal static class Recycler_StartRecycling_Patch
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codeInstructions = instructions.ToList();

                for (int i = 0; i < codeInstructions.Count; i++)
                {
                    if (codeInstructions[i].opcode == OpCodes.Ldc_R4)
                    {
                        codeInstructions[i].operand = _recyclerSpeed;
                    }

                    yield return codeInstructions[i];
                }
            }

        }
    }
}