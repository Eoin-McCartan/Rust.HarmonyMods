// Reference: 0Harmony

using System;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CustomSave", "Strobez", "1.0.0")]
    internal class CustomSave : RustPlugin
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

        [HarmonyPatch(typeof(SaveRestore), "DoAutomatedSave", typeof(bool))]
        internal static class SaveRestore_DoAutomatedSave_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix()
            {
                ConVar.Chat.Broadcast("<color=#FF0000>WARNING!</color> Server will save in <color=#FF0000>5</color> seconds");

                Task.Delay(5000).Wait();

                int entityCount = BaseNetworkable.serverEntities.Count;

                ConVar.Chat.Broadcast($"<color=#FF0000>WARNING!</color> Server is now saving <color=#f88122>{entityCount}</color> entities - You might experience some lag.");

                return true;
            }
        }
    }
}