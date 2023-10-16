// Reference: 0Harmony

using System;
using Harmony;
using Network;
using Oxide.Core;
using Oxide.Core.Libraries;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Bypass Queue", "Strobez", "1.0.0")]
    internal class BypassQueue : RustPlugin
    {
        private static HarmonyInstance? _harmonyInstance;

        private void OnServerInitialized()
        {
            try
            {
                _harmonyInstance = HarmonyInstance.Create(Name + "Patches");
                _harmonyInstance.PatchAll();
                
                permission.RegisterPermission("bypassqueue.allow", this);
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
        
        [HarmonyPatch(typeof(ConnectionQueue), "CanJumpQueue")]
        internal class ConnectionQueue_CanJumpQueue_Patch
        {
            internal static readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
            
            [HarmonyPrefix]
            internal static bool Prefix(Connection __instance, ref bool __result)
            {
                if (permission.UserHasPermission(__instance.userid.ToString(), "bypassqueue.allow"))
                {
                    __result = true;
                    return false;
                }
                
                return true;
            }
        }
    }
}