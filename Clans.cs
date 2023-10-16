// Reference: 0Harmony

// WIP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Harmony;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Clans", "Strobez & 14teemo", "0.0.1")]
    public class Clans : RustPlugin
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
        
        #region Clan
        public class Clan
        {
            private Clan(
                string tag, 
                string ownerID, 
                HashSet<string> moderators, 
                HashSet<string> members,
                HashSet<string> invited, 
                bool friendlyFire)
            {
                Tag = tag;
                OwnerID = ownerID;
                Moderators = moderators;
                Members = members;
                Invited = invited;
                FriendlyFire = friendlyFire;
            }

            public string Tag { get; }
            public string OwnerID { get; }
            public HashSet<string> Moderators { get; }
            public HashSet<string> Members { get; }
            public HashSet<string> Invited { get; }
            public bool FriendlyFire { get; set; }

            public static Clan Create(string tag, string ownerID, HashSet<string>? moderators = null,
                HashSet<string>? members = null, HashSet<string>? invited = null, bool friendlyFire = false)
            {
                return new Clan(tag, ownerID, moderators ?? new HashSet<string>(), members ?? new HashSet<string> {ownerID},
                    invited ?? new HashSet<string>(), friendlyFire);
            }

            public void Broadcast(string message)
            {
                foreach (BasePlayer player in Members.Select(BasePlayer.Find).Where(player => player != null))
                {
                    player.ChatMessage(message);
                }
            }
        }
        #endregion

        #region Defines
        private static readonly Dictionary<string, Clan> _clans = new(StringComparer.InvariantCultureIgnoreCase);
        private static readonly Dictionary<ulong, DateTime> _cinfoCooldowns = new();
        #endregion

        #region Commands
        [ChatCommand("cinfo")]
        private void CmdClanInfo(BasePlayer player, string command, string[] args)
        {
            GetClanInfo(player, args, true);
        }

        [ChatCommand("clan")]
        private void CmdClan(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                Reply(
                    $"You have entered an invalid command!\nPlease use {CommandHint("/clan help")} to view a list of available commands."
                );
                
                return;
            }
            
            Clan? clan = FindClanByMember(player.UserIDString);

            switch (args[0])
            {
                case "help":
                    Reply("TODO");
                    break;
                case "info":
                    GetClanInfo(player, args, false);
                    
                    break;
                case "create":
                    if (clan is not null)
                    {
                        Reply("You are already in a clan.");
                    }
                    
                    if (args.Length != 2)
                    {
                        Reply($"To create a clan, use {CommandHint("/clan create <tag>")}");
                        return;
                    }

                    string tag = args[1];
                    
                    if (tag.Length > 5)
                    {
                        Reply("Clan tags cannot be longer than 5 characters.");
                        return;
                    }
                    
                    if (!Regex.IsMatch(tag, "^[a-zA-Z]+$"))
                    {
                        Reply("You cannot to use special characters or numbers in your clan tag.");
                        return;
                    }
                    
                    // TODO Banned words filter

                    if (_clans.ContainsKey(tag))
                    {
                        Reply($"A clan with the tag {ClanTag(tag)} already exists!");
                        return;
                    }

                    if (!_clans.TryAdd(tag, Clan.Create(tag, player.UserIDString)))
                    {
                        Reply("An error occurred while creating clan.");
                        return;
                    }
                    
                    Reply($"You have successfully created a clan with the tag {ClanTag(tag)}!");
                    break;
                case "join":
                    if (clan is not null)
                    {
                        Reply("You are already in a clan.");
                    }
                     
                    if (args.Length != 2)
                    {
                        Reply(
                            $"To join a clan you have been invited to, use ${CommandHint("/clan join <tag>")}"
                        );
                        return;
                    }

                    string targetTag = args[1];
                    
                    Clan? targetClan = FindClan(targetTag);
                    
                    if (targetClan == null)
                    {
                        Reply($"Cannot find a clan with the tag {targetTag}.");
                        return;
                    }

                    if (targetClan.IsMember(player.UserIDString))
                    {
                        Reply($"You are already a member of {targetTag}.");
                        return;
                    }

                    if (!targetClan.IsInvited(player.UserIDString))
                    {
                        Reply($"You have not been invited to join {targetTag}.");
                        return;
                    }

                    if (targetClan.Members.Count >= config.MaxClanSize)
                    {
                        Reply(
                            $"Cannot join {targetTag} as it has reached the maximum size of {config.MaxClanSize} players."
                        );
                        
                        return;
                    }

                    targetClan.Members.Add(player.UserIDString);
                    
                    targetClan.Broadcast($"{player.displayName} has joined the clan!");
                    break;
                case "invite":
                    if (clan is null)
                    {
                        Reply("You are not in a clan.");
                        return;
                    }

                    if (args.Length != 2)
                    {
                        Reply($"To invite a player to the clan, use {CommandHint("/clan invite <name>")}");
                        return;
                    }
                    
                    if (!clan.IsModerator(player.UserIDString))
                    {
                        Reply("You do not have permission to invite players to the clan.");
                        return;
                    }

                    string target = args[1];
                    
                    BasePlayer? targetPlayer = BasePlayer.Find(target);
                    
                    if (targetPlayer is null)
                    {
                        Reply($"Cannot find a player with the name {target}");
                        return;
                    }

                    if (FindClanByMember(targetPlayer.UserIDString) is not null)
                    {
                        Reply($"{target} is already in a clan.");
                        return;
                    }

                    if (clan.IsInvited(targetPlayer.UserIDString))
                    {
                        Reply($"{target} already has a pending invite.");
                        return;
                    }

                    clan.Invited.Add(targetPlayer.UserIDString);
                    
                    targetPlayer.ChatMessage($"You have been invited to join clan {ClanTag(clan.Tag)}");

                    break;
                case "disband":
                    if (clan == null || !clan.IsOwner(player.UserIDString))
                    {
                        Reply(
                            "You have entered an invalid command!\nPlease use /clan help to view a list of available commands.");
                        return;
                    }
                    
                    if (!_clans.Remove(clan.Tag, out _))
                    {
                        Reply("An error occurred while disbanding the clan.");
                    }
                    
                    clan.Broadcast($"The clan {clan.Tag} has been disbanded!");
                    
                    break;
                case "promote":
                    if (clan == null || !clan.IsOwner(player.UserIDString) || args.Length == 1)
                    {
                        Reply(
                            "You have entered an invalid command!\nPlease use /clan help to view a list of available commands.");
                        return;
                    }

                    string promoteTarget = args[1];
                    BasePlayer? promoteTargetPlayer = BasePlayer.Find(promoteTarget);
                    if (promoteTargetPlayer == null)
                    {
                        Reply($"Cannot find a player with the name {promoteTarget}!");
                        return;
                    }

                    if (clan.IsOwner(promoteTargetPlayer.UserIDString) ||
                        clan.IsModerator(promoteTargetPlayer.UserIDString))
                    {
                        Reply($"Cannot promote {promoteTargetPlayer.displayName} to Moderator!");
                        return;
                    }

                    clan.Moderators.Add(promoteTargetPlayer.UserIDString);
                    clan.Broadcast($"{promoteTargetPlayer.displayName} has been promoted to Moderator!");
                    promoteTargetPlayer.ChatMessage($"You have been promoted to Moderator of the clan {clan.Tag}!");
                    break;
                case "demote":
                    if (clan == null || !clan.IsOwner(player.UserIDString) || args.Length == 1)
                    {
                        Reply(
                            "You have entered an invalid command!\nPlease use /clan help to view a list of available commands.");
                        return;
                    }

                    string demoteTarget = args[1];
                    BasePlayer? demoteTargetPlayer = BasePlayer.Find(demoteTarget);
                    if (demoteTargetPlayer == null)
                    {
                        Reply($"Cannot find a player with the name {demoteTarget}!");
                        return;
                    }

                    if (!clan.IsModerator(demoteTargetPlayer.UserIDString) || clan.IsOwner(demoteTargetPlayer.UserIDString))
                    {
                        Reply($"Cannot demote {demoteTargetPlayer.displayName} to Member!");
                        return;
                    }

                    clan.Moderators.Remove(demoteTargetPlayer.UserIDString);
                    clan.Broadcast($"{demoteTargetPlayer.displayName} has been demoted to Member!");
                    demoteTargetPlayer.ChatMessage($"You have been demoted to Member of the clan {clan.Tag}!");
                    break;
                case "kick":
                    if (clan == null || !clan.IsModerator(player.UserIDString) || args.Length == 1)
                    {
                        Reply(
                            "You have entered an invalid command!\nPlease use /clan help to view a list of available commands.");
                        return;
                    }

                    string kickTarget = args[1];
                    BasePlayer? kickTargetPlayer = BasePlayer.Find(kickTarget);
                    if (kickTargetPlayer == null)
                    {
                        Reply("Cannot find a player with the name {kickTarget}!");
                        return;
                    }

                    if (clan.IsOwner(kickTargetPlayer.UserIDString) ||
                        !clan.IsMember(kickTargetPlayer.UserIDString) ||
                        clan.IsModerator(kickTargetPlayer.UserIDString))
                    {
                        Reply($"Cannot kick {kickTargetPlayer.displayName} from clan!");
                        return;
                    }

                    clan.Members.Remove(kickTargetPlayer.UserIDString);
                    clan.Broadcast($"{kickTargetPlayer.displayName} has been kicked from the clan!");
                    kickTargetPlayer.ChatMessage($"You have been kicked from the clan {ClanTag(clan.Tag)}!");
                    break;
                case "leave":
                    if (clan is null)
                    {
                        Reply("You are not in a clan.");

                        return;
                    }

                    if (clan.IsOwner(player.UserIDString))
                    {
                        Reply(
                            "You cannot leave the clan as you are the only member!\nPlease use /clan disband instead.");
                        return;
                    }

                    clan.Members.Remove(player.UserIDString);
                    clan.Broadcast($"{player.displayName} has left the clan!");
                    Reply($"You are no longer apart of the clan {ClanTag(clan.Tag)}!");

                    break;
                case "ff":
                    if (clan == null)
                    {
                        Reply("You are not currently in a clan.");
                        return;
                    }

                    if (!clan.IsModerator(player.UserIDString))
                    {
                        Reply(
                            "You must be the owner or a moderator of the clan to change friendly fire settings.");
                        return;
                    }

                    bool friendlyFire = !clan.IsFriendlyFire();
                    clan.FriendlyFire = friendlyFire;
                    clan.Broadcast($"Friendly fire has been {(friendlyFire ? "enabled" : "disabled")}.");
                    break;
                default:
                    Reply(
                        $"You have entered an invalid command!\nPlease use {CommandHint("/clan help")} to view a list of available commands."
                    );
                    break;
            }

            return;

            void Reply(string message)
            {
                player.ChatMessage(message);
            }
        }
        #endregion

        #region Methods

        private void GetClanInfo(BasePlayer player, string[] args, bool isCinfo)
        {
            if (_cinfoCooldowns.TryGetValue(player.userID, out DateTime cooldown))
            {
                TimeSpan timeSpan = DateTime.Now - cooldown;
                if (timeSpan.TotalSeconds < config.ClanInfoTimeout)
                {
                    TimeSpan remainingTime = TimeSpan.FromSeconds(config.ClanInfoTimeout - timeSpan.TotalSeconds);

                    string timeLeft = remainingTime.TotalMinutes > 1
                        ? $"{Math.Floor(remainingTime.TotalMinutes)}m {remainingTime.Seconds}s"
                        : $"{remainingTime.Seconds}s";

                    player.ChatMessage(
                        $"You must wait {timeLeft} before using this command again.");
                    return;
                }

                _cinfoCooldowns.Remove(player.userID);
            }
            
            Clan? clan = args.Length == (isCinfo ? 0 : 2)
                ? FindClanByMember(player.userID.ToString()) 
                : FindClan(args[0]);

            if (clan is null)
            {
                player.ChatMessage("Clan not found.");
                return;
            }

            List<string> onlineMembers = new();
            List<string> offlineMembers = new();

            foreach (string member in clan.Members)
            {
                BasePlayer onlinePlayer = BasePlayer.Find(member);
                if (onlinePlayer != null)
                {
                    onlineMembers.Add(onlinePlayer.displayName);
                }

                BasePlayer offlinePlayer = BasePlayer.FindSleeping(member);
                if (offlinePlayer != null)
                {
                    offlineMembers.Add(offlinePlayer.displayName);
                }
            }

            StringBuilder sb = new();

            sb.AppendLine(
                $"You are viewing the information for: {ClanTag(clan.Tag)} ({clan.Members.Count}/{config.MaxClanSize})"
            );

            BasePlayer? owner = BasePlayer.Find(clan.OwnerID);

            if (owner is not null)
            {
                sb.AppendLine($"Owner: {owner.displayName}");
            }

            sb.AppendLine($"Moderators: {string.Join(", ", clan.Moderators.Select(x => BasePlayer.Find(x)?.displayName))}");
            sb.AppendLine($"Members online: {string.Join(", ", onlineMembers)}");
            sb.AppendLine($"Members offline: {string.Join(", ", offlineMembers)}");

            player.ChatMessage(sb.ToString());
            
            _cinfoCooldowns.Add(player.userID, DateTime.Now);
        }

        #endregion

        #region API
        private static Clan? FindClan(string tag)
        {
            return _clans.TryGetValue(tag, out Clan? clan) ? clan : null;
        }

        private static Clan? FindClanByOwner(string ownerID)
        {
            foreach (Clan clan in _clans.Values)
            {
                if (clan.OwnerID == ownerID) return clan;
            }

            return null;
        }

        private static Clan? FindClanByMember(string memberID)
        {
            foreach (Clan clan in _clans.Values)
            {
                if (clan.IsMember(memberID)) return clan;
            }

            return null;
        }
        
        private static void InviteToClan(Clan clan, BasePlayer player)
        {
            clan.Invited.Add(player.UserIDString);
                
            player.ChatMessage($"You have been invited to join clan {ClanTag(clan.Tag)}");
        }
        
        #endregion

        #region Configuration
        private Configuration config;

        private class Configuration
        {
            [JsonProperty("Max Clan Size")] 
            public int MaxClanSize = 10;
            
            [JsonProperty("Clan Info Timeout (Seconds)")] 
            public int ClanInfoTimeout = 120;

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }

            public Dictionary<string, object> ToDictionary()
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new JsonException();

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }
        #endregion Configuration

        #region Helpers

        private static string CommandHint(string command)
        {
            return $"<color=#82b2ff>{command}</color>";
        }
        
        private static string ClanTag(string tag)
        {
            return $"<color=#dfff6b>[{tag}]</color>";
        }

        #endregion

        #region Harmony Patches

        [HarmonyPatch(typeof(RelationshipManager), "trycreateteam")]
        internal class RelationshipManager_trycreateteam_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ConsoleSystem.Arg __instance)
            {
                BasePlayer? player = __instance.Player();

                player?.ChatMessage($"To create a clan use {CommandHint("/clan create <tag>")}");

                return false;
            }
        }
        
        [HarmonyPatch(typeof(RelationshipManager), nameof(RelationshipManager.DisbandTeam))]
        internal class RelationshipManager_DisbandTeam_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ConsoleSystem.Arg __instance)
            {
                BasePlayer? player = __instance.Player();

                if (player is not null)
                {
                    player.ChatMessage($"To disband a clan use {CommandHint("/clan disband")}");
                }

                return false;
            }
        }
        
        [HarmonyPatch(typeof(RelationshipManager), nameof(RelationshipManager.leaveteam))]
        internal class RelationshipManager_leaveteam_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ConsoleSystem.Arg __instance)
            {
                BasePlayer? player = __instance.Player();
                if (player is null) return true;
                
                Clan? clan = FindClanByMember(player.UserIDString);
                
                if (clan is null)
                {
                    player.ChatMessage("You are not in a clan.");

                    return true;
                }

                clan.Members.Remove(player.UserIDString);
                clan.Broadcast($"{player.displayName} has left the clan!");
                player.ChatMessage($"You are no longer apart of the clan {ClanTag(clan.Tag)}!");
                
                return true;
            }
        }

        [HarmonyPatch(typeof(RelationshipManager.PlayerTeam), nameof(RelationshipManager.PlayerTeam.SendInvite))]
        internal class RelationshipManager_PlayerTeam_SendInvite_Patch
        {
            [HarmonyPostfix]
            private static bool Postfix(BasePlayer player, RelationshipManager.PlayerTeam __instance)
            {
                KeyValuePair<string, Clan>? clan = _clans.FirstOrDefault(
                    o => o.Value.OwnerID == __instance.teamLeader.ToString());

                if (!clan.HasValue) return false;

                InviteToClan(clan.Value.Value, player);
                
                return true;
            }
        }

        #endregion
    }
    
    #region Extensions
    public static class ClanExtensions
    {
        public static bool IsOwner(this Clans.Clan clan, string playerID)
        {
            return clan.OwnerID == playerID;
        }

        public static bool IsModerator(this Clans.Clan clan, string playerID)
        {
            return clan.IsOwner(playerID) || clan.Moderators.Contains(playerID);
        }

        public static bool IsMember(this Clans.Clan clan, string playerID)
        {
            return clan.Members.Contains(playerID);
        }

        public static bool IsInvited(this Clans.Clan clan, string playerID)
        {
            return clan.Invited.Contains(playerID);
        }

        public static bool IsFriendlyFire(this Clans.Clan clan)
        {
            return clan.FriendlyFire;
        }
    }
    #endregion

}