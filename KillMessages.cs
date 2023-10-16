// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Kill Messages", "Strobez", "0.0.2")]
    internal class KillMessages : RustPlugin
    {

        #region Defines
        private const string KILL_MESSAGE_TEMPLATE =
            "<color=#B73C40>{0}</color> <color=#DCDCDC>|</color> <color=#5893D9>{1}</color>\n<color=#DCDCDC>{2} | {3} meters | {4}</color>";
        #endregion

        #region Harmony
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

        [HarmonyPatch(typeof(BasePlayer), nameof(BasePlayer.Die))]
        internal static class BasePlayer_Die_Patch
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo? methodInfo = typeof(BasePlayer_Die_Patch).GetMethod(nameof(PatchDeathWithKillMessage));

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode.Equals(OpCodes.Ldstr) && instruction.operand.Equals("OnPlayerDeath"))
                    {
                        yield return instruction;

                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, methodInfo);

                        continue;
                    }

                    yield return instruction;
                }
            }

            public static void PatchDeathWithKillMessage(BasePlayer entity, HitInfo info)
            {
                if (entity == null) return;

                if (entity.IsNpc || !entity.userID.IsSteamId() || entity.UserIDString.Length != 17) return;

                BasePlayer attacker = info.InitiatorPlayer;

                if (attacker == null) return;

                if (attacker.IsNpc || !attacker.userID.IsSteamId() || attacker.UserIDString.Length != 17) return;

                if (IsExplosion(info))
                {
                    DeathFromExplosion(entity, attacker, info);
                    return;
                }

                if (info.damageTypes.GetMajorityDamageType() == DamageType.Heat ||
                    !info.damageTypes.IsBleedCausing() && info.damageTypes.Has(DamageType.Heat))
                {
                    DeathFromBurning(entity, attacker);
                    return;
                }

                string distance = GetDistance(entity, info);
                if (distance == null) return;

                if (!info.IsProjectile())
                {
                    DeathFromMelee(entity, attacker, info, distance);
                    return;
                }

                DeathFromProjectile(entity, attacker, info, distance);
            }

            #region Message Methods
            internal static void DeathFromExplosion(BasePlayer victim, BasePlayer attacker, HitInfo info)
            {
                string message = string.Format(KILL_MESSAGE_TEMPLATE, attacker.displayName, victim.displayName, "Body",
                    "0",
                    "Explosion");

                NotifyPlayers(attacker, victim, message);
            }

            internal static void DeathFromBurning(BasePlayer victim, BasePlayer attacker)
            {
                string message = string.Format(KILL_MESSAGE_TEMPLATE, attacker.displayName, victim.displayName, "Body",
                    "0",
                    "Fire");

                NotifyPlayers(attacker, victim, message);
            }

            internal static void DeathFromMelee(BasePlayer victim, BasePlayer attacker, HitInfo info, string dist)
            {
                AttackEntity wpn = info.Weapon;
                string lastShotLoc = info.boneName;
                lastShotLoc = lastShotLoc.First().ToString().ToUpper() + lastShotLoc[1..];

                if (wpn == null ||
                    wpn.GetItem() == null ||
                    wpn.GetItem().info == null ||
                    wpn.GetItem().info.displayName == null ||
                    wpn.GetItem().info.displayName.english == null) return;

                string message = string.Format(KILL_MESSAGE_TEMPLATE, attacker.displayName, victim.displayName,
                    lastShotLoc,
                    dist,
                    wpn.GetItem().info.displayName.english);

                NotifyPlayers(attacker, victim, message);
            }

            internal static void DeathFromProjectile(BasePlayer victim, BasePlayer attacker, HitInfo info, string dist)
            {
                AttackEntity wpn = info.Weapon;
                string lastShotLoc = info.boneName;
                lastShotLoc = lastShotLoc.First().ToString().ToUpper() + lastShotLoc[1..];


                if (wpn == null ||
                    wpn.GetItem() == null ||
                    wpn.GetItem().info == null ||
                    wpn.GetItem().info.displayName == null ||
                    wpn.GetItem().info.displayName.english == null) return;

                string message = string.Format(KILL_MESSAGE_TEMPLATE, attacker.displayName, victim.displayName,
                    lastShotLoc,
                    dist,
                    wpn.GetItem().info.displayName.english);

                NotifyPlayers(attacker, victim, message);
            }

            internal static void NotifyPlayers(BasePlayer? attacker, BasePlayer victim, string message)
            {
                victim.ChatMessage(message);
                attacker?.ChatMessage(message);
            }
            #endregion

            #region Helper Methods
            internal static string GetDistance(BaseCombatEntity entity, HitInfo info)
            {
                float distance = 0.0f;

                if (entity != null && info.Initiator != null)
                    distance = Vector3.Distance(info.Initiator.transform.position, entity.transform.position);
                return distance.ToString("0").Equals("0") ? "" : distance.ToString("0");
            }

            internal static bool IsExplosion(HitInfo hit)
            {
                return hit.WeaponPrefab != null &&
                       (hit.WeaponPrefab.ShortPrefabName.Contains("grenade") ||
                        hit.WeaponPrefab.ShortPrefabName.Contains("explosive")) ||
                       hit.damageTypes.GetMajorityDamageType() == DamageType.Explosion ||
                       !hit.damageTypes.IsBleedCausing() && hit.damageTypes.Has(DamageType.Explosion);
            }
            #endregion

        }
        #endregion

    }
}