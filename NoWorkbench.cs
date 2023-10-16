// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Oxide.Plugins;
using Harmony;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("No Workbench", "Strobez", "1.0.0")]
	internal class NoWorkbench : RustPlugin
	{
		private static HarmonyInstance? _harmonyInstance;

		private void OnServerInitialized()
		{
			try
			{
				_harmonyInstance = HarmonyInstance.Create(Name + "Patchs");
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

		[HarmonyPatch(typeof(ItemModRepair), "HasCraftLevel")]
		internal static class ItemModRepair_HasCraftLevel_Patch
		{
			[HarmonyPrefix]
			private static bool Prefix(ItemModRepair __instance, BasePlayer player, ref bool __result)
			{
				__result = true;
				return false;
			}
		}

		[HarmonyPatch(typeof(BasePlayer), "PlayerInit")]
		internal class BasePlayer_PlayerInit_Patch
		{
			[HarmonyPostfix]
			private static void Postfix(BasePlayer __instance)
			{
				__instance.ClientRPCPlayer(null, __instance, "craftMode", 1);
			}
		}

		[HarmonyPatch(typeof(Bootstrap), "StartupShared")]
		internal class Bootstrap_StartupShared_Patch
        {
			[HarmonyPostfix]
			private static void Postfix()
			{
				foreach (ItemBlueprint blueprint in ItemManager.GetBlueprints())
				{
					blueprint.workbenchLevelRequired = 0;
				}
			}
		}
	}
}
