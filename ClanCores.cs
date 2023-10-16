// WIP

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ClanCores", "Strbez", "0.0.1")]
    public class ClanCores : RustPlugin
    {
        [PluginReference] private Plugin Clans;

        #region Commands
        [ChatCommand("csetcore")]
        private void CmdSetCore(BasePlayer player, string command, string[] args)
        {
            string? clanTag = GetClanTag(player.UserIDString);
            if (clanTag == null)
            {
                player.ChatMessage("You must be in a clan to set a clan core!");
                return;
            }
            
            if (_data.ContainsKey(clanTag))
            {
                player.ChatMessage("You already have a clan core set!");
                return;
            }

            if (!player.CanBuild())
            {
                player.ChatMessage("You must be authorized on a cupboard to set a clan core!");
                return;
            }

            if (Physics.Raycast(player.eyes.HeadRay(), out RaycastHit hitInfo, 100f))
            {
                BaseEntity entity = hitInfo.GetEntity();

                if (entity is not VendingMachine)
                {
                    player.ChatMessage("You must be looking at a vending machine!");
                    return;
                }

                _data.Add(clanTag, new ClanCore(clanTag, entity.transform.position));

                player.ChatMessage("You have created a clan core, you can now start collecting points!");
            }
        }
        #endregion

        #region Configuration
        private Configuration config;

        private class Configuration
        {
            [JsonProperty("Percentage of points lost per every hour with no core set")]
            public float PointsLostPerHour { get; set; } = 10.0f;

            [JsonProperty("Points Gained Per Category (online raiding & offline raiding are percentages)")]
            public Dictionary<GainedPoints, int> PointsPerCategory { get; set; } = new()
            {
                {GainedPoints.Kills, 100},
                {GainedPoints.Koth, 25000},
                {GainedPoints.LastDamageHeli, 2000},
                {GainedPoints.LastDamageBradley, 2000},
                {GainedPoints.HeliCrate, 1500},
                {GainedPoints.BradleyCrate, 1500},
                {GainedPoints.OnlineRaiding, 50},
                {GainedPoints.OfflineRaiding, 25}
            };

            [JsonProperty("Points Lost Per Category (all percentages)")]
            public Dictionary<LostPoints, int> PointsLostPerCategory { get; set; } = new()
            {
                {LostPoints.OnlineRaiding, 50},
                {LostPoints.OfflineRaiding, 25},
                {LostPoints.SelfDestruction, 50},
                {LostPoints.BaseDecay, 50}
            };

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
        #endregion

        #region Clan Core
        public Dictionary<string, ClanCore> _data = new();

        public enum GainedPoints
        {
            Kills,
            Koth,
            LastDamageHeli,
            LastDamageBradley,
            HeliCrate,
            BradleyCrate,
            OnlineRaiding,
            OfflineRaiding
        }

        public enum LostPoints
        {
            OnlineRaiding,
            OfflineRaiding,
            SelfDestruction,
            BaseDecay
        }

        public class ClanCore
        {

            public ClanCore(string? clanTag, Vector3 position)
            {
                ClanTag = clanTag;
                Position = position;
                PointsGained = new Dictionary<GainedPoints, int>();
                PointsLost = new Dictionary<LostPoints, int>();
            }

            public string? ClanTag { get; set; }

            public Vector3 Position { get; set; }

            public Dictionary<GainedPoints, int> PointsGained { get; set; }

            public Dictionary<LostPoints, int> PointsLost { get; set; }
        }

        private void DecayPoints()
        {
    
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("ClanCores", _data);
        }

        private void LoadData()
        {
            _data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, ClanCore>>("ClanCores");
        }
        #endregion

        #region Oxide Hooks
        private void OnServerSave()
        {
            SaveData();
        }

        private void OnServerInitialized()
        {
            LoadData();
        }

        private void Unload()
        {
            SaveData();
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || player.IsNpc)
                return;

            BasePlayer attacker = info.InitiatorPlayer;
            if (attacker == null || attacker.IsNpc)
                return;

            string? clanTag = GetClanTag(player.UserIDString);
            if (clanTag == null)
                return;

            if (!_data.ContainsKey(clanTag))
                return;

            if (IsClanMember(player.UserIDString, attacker.UserIDString))
                return;

            _data[clanTag].PointsGained[GainedPoints.Kills] += config.PointsPerCategory[GainedPoints.Kills];
        }
        #endregion

        #region Clan API
        private string? GetClanTag(string userId)
        {
            string? clanTag = Clans?.Call<string>("GetClanOf", userId);

            return clanTag;
        }

        private bool IsClanMember(string userId, string otherId)
        {
            bool isMember = Clans?.Call<bool>("IsClanMember", userId, otherId) ?? false;

            return isMember;
        }
        #endregion

    }
}