// Reference: 0Harmony

using System;
using Harmony;
using UnityEngine;
using static ConVar.Chat;

namespace Oxide.Plugins
{
    [Info("No Give Notices", "Strobez", "1.0.0")]
    internal class NoGiveNotices : RustPlugin
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

        [HarmonyPatch(typeof(ConVar.Chat), nameof(Broadcast), typeof(string), typeof(string), typeof(string), typeof(ulong))]
        internal static class Chat_Broadcast_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(string message, string username = "SERVER", string color = "#eee", ulong userid = 0)
            {
                return !message.Contains("gave") || username != "SERVER";
            }
        }
    }
}