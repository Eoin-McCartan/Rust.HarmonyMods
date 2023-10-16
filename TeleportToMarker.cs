// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Teleport To Marker", "Strobez / 14teemo", "0.0.2")]
    internal sealed class TeleportToMarker : RustPlugin
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

        [HarmonyPatch(typeof(BasePlayer), nameof(BasePlayer.Server_AddMarker))]
        internal static class BasePlayer_Server_AddMarker_Patch
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo? methodInfo = typeof(BasePlayer_Server_AddMarker_Patch).GetMethod(nameof(TeleportPlayer));
        
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Call, methodInfo);
            
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                }
            }
        
            public static void TeleportPlayer(BasePlayer player, BaseEntity.RPCMessage msg)
            {
                if (player.net.connection.authLevel != 2)
                    return;

                MapNote n = MapNote.Deserialize(msg.read);
                Vector3 position = n.worldPosition + new Vector3(0, 160, 0);
            
                player.Teleport(position);
            
                n.ResetToPool();
            }
        }
    }
}