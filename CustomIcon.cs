// Reference: 0Harmony

using System;
using System.Collections.Generic;
using Harmony;
using Network;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Icon", "Strobez", "1.0.0")]
    internal class CustomIcon : RustPlugin
    {
        private static HarmonyInstance? _harmonyInstance;
        private const ulong SteamAvatarUserID = 76561199040886468;

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

        [HarmonyPatch(typeof(ConsoleNetwork), nameof(ConsoleNetwork.BroadcastToAllClients), typeof(string), typeof(object[]))]
        internal static class ConsoleNetwork_BroadcastToAllClients_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(string strCommand, object[] args)
            {
                TryApplySteamAvatarUserID(strCommand, args);
                
                return true;
            }
        }


        [HarmonyPatch(typeof(ConsoleNetwork), nameof(ConsoleNetwork.SendClientCommand), typeof(Connection), typeof(string), typeof(object[]))]
        internal static class ConsoleNetwork_SendClientCommand_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(Connection cn, string strCommand, object[] args)
            {
                TryApplySteamAvatarUserID(strCommand, args);

                return true;
            }
        }

        [HarmonyPatch(typeof(ConsoleNetwork), nameof(ConsoleNetwork.SendClientCommand), typeof(List<Connection>), typeof(string), typeof(object[]))]
        internal static class ConsoleNetwork_SendClientCommand_List_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix(List<Connection> cn, string strCommand, object[] args)
            {
                TryApplySteamAvatarUserID(strCommand, args);

                return true;
            }
        }
        
        private static void TryApplySteamAvatarUserID(string command, object[] args)
        {
            if (args is null)
                return;
                
            if (args.Length < 2 || command != "chat.add" && command != "chat.add2") 
                return;
                    
            if (ulong.TryParse(args[1].ToString(), out ulong providedID) && providedID == 0)
                args[1] = SteamAvatarUserID;
        }
    }
}