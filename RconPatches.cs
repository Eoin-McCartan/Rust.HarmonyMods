// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ConVar;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins;

[Info("Rcon Patches", "Strobez / 14teemo", "1.0.0")]
public sealed class RconPatches : RustPlugin
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
    
    [HarmonyPatch(typeof(Admin), "mute")]
    internal static class Admin_mute_Patch
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? methodInfo = typeof(Admin_mute_Patch).GetMethod(nameof(PatchMuteWithBetterResponse));
            
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt 
                    && (instruction.operand as MemberInfo)?.Name == nameof(BasePlayer.SetPlayerFlag))
                {
                    yield return instruction;
                    
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, methodInfo);

                    continue;
                }

                yield return instruction;
            }
        }

        public static void PatchMuteWithBetterResponse(ConsoleSystem.Arg arg)
        {
            BasePlayer playerOrSleeper = arg.GetPlayerOrSleeper(0);
            
            arg.ReplyWith($"{playerOrSleeper.UserIDString} muted successfully");
        }
    }
    
    [HarmonyPatch(typeof(BasePlayer), nameof(BasePlayer.SetPlayerFlag))]
    internal static class BasePlayer_SetPlayerFlag_Patch
    {
        [HarmonyPostfix]
        internal static void Postfix(BasePlayer __instance, BasePlayer.PlayerFlags f, bool b)
        {
            if (f == BasePlayer.PlayerFlags.ChatMute && b)
            {
                __instance.ChatMessage("You have been muted permanently.");
            }
        }
    }
}