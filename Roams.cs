using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Facepunch;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using Rust;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Roams", "Strobez", "1.0.2")]
    internal class Roams : RustPlugin
    {

        #region Commands
        [ChatCommand("roams")]
        private async void CmdRoams(BasePlayer? player, string command, string[] args)
        {
            if (player == null)
                return;

            if (_activeRoam == null)
            {
                SendReply(player,
                    "<color=#5557AA>Teleportation</color>\nThere must be an active roam to use this command!");
                return;
            }

            if (_playersInZone.Contains(player.userID))
            {
                SendReply(player, "<color=#5557AA>Teleportation</color>\nYou are already in the roam zone!");
                return;
            }

            if (_cooldowns.TryGetValue(player.userID, out DateTime cooldownTime))
                if (cooldownTime.AddMinutes(2) > DateTime.Now)
                {
                    TimeSpan timeLeft = cooldownTime.AddMinutes(2) - DateTime.Now;
                    SendReply(player,
                        $"You must wait <color=#5557AA>{timeLeft.Minutes:D1} minutes</color> and <color=#5557AA>{timeLeft.Seconds:D1} seconds</color> before you can use this command again!");
                    return;
                }

            KeyValuePair<string, Vector3> roam = _activeRoam.ElementAt(0);

            for (int i = 0; i < 11; i++)
            {
                switch (i)
                {
                    case 10:
                        player.Teleport(roam.Value);
                        player.ChatMessage(
                            $"<color=#5557AA>Teleportation</color>\nYou have been teleported to the <color=#5557AA>{roam.Key}</color>!");
                        break;
                    case 0:
                        player.ChatMessage(
                            $"<color=#5557AA>Teleportation</color>\nYou will be teleported to the <color=#5557AA>{roam.Key}</color> in <color=#5557AA>{10 - i} seconds</color>!");
                        break;
                    default:
                        if (i >= 7)
                            player.ChatMessage(
                                $"<color=#5557AA>Teleportation</color>\nYou will be teleported to the <color=#5557AA>{roam.Key}</color> in <color=#5557AA>{10 - i} seconds</color>!");
                        break;
                }

                await Task.Delay(1000);
            }

            _cooldowns[player.userID] = DateTime.Now;
        }
        #endregion

        #region Defines
        public static readonly Dictionary<string, Vector3> RoamIslands = new();
        public static readonly Dictionary<string?, ClanStatistics> _clanStats = new();
        private const float _safeDuration = 300f;
        public const float RoamInterval = 2700f;
        public const float RoamDuration = 900f;
        private static readonly Dictionary<string, Vector3> _activeRoam = new();
        private static readonly Dictionary<ulong, DateTime> _cooldowns = new();

        [PluginReference] private Plugin Clans;
        private readonly List<ulong> _playersInZone = new();
        private readonly Dictionary<ulong, Timer> _playerUiTimers = new();

        private static Roams _instance = new();
        #endregion

        #region Core
        private void CreateRoam(string roamType, Vector3 position)
        {
            _instance.SendGlobalMessage(
                $"A new roam event has started at <color=#5557AA>{roamType}</color>!\n<size=12>Use <color=#ACFA58>/roams</color> to join!</size>");
            Debug.Log($"[Roams] A new roam event has started at {roamType}| Auto-Generated | Timestamp: {DateTime.Now}");

            GameObject go = new();
            go.AddComponent<ZoneBehaviour>().Position = position;
            go.GetComponent<ZoneBehaviour>().Radius = 250f;
            go.GetComponent<ZoneBehaviour>().Duration = RoamDuration;
        }

        private async void HandleRoamInterval()
        {
            if (RoamIslands.Count == 0)
                return;

            int idx = Random.Range(0, RoamIslands.Count);
            KeyValuePair<string, Vector3> roamIsland = RoamIslands.ElementAt(idx);

            _activeRoam.Add(roamIsland.Key, roamIsland.Value);

            for (int i = 0; i < 10; i++)
            {
                if (i == 9)
                {
                    CreateRoam(roamIsland.Key, roamIsland.Value);
                }
                else
                {
                    _instance.SendGlobalMessage(
                        $"A roam is starting in {FormatTime(_safeDuration - i * 30)}!\n<size=12>Use <color=#ACFA58>/roams</color> to join!</size>");
                    Debug.Log(
                        $"[Roams] A roam is starting in {FormatTime(_safeDuration - i * 30)}| Auto-Generated | Timestamp: {DateTime.Now}");
                }

                await Task.Delay(30000);
            }
        }
        #endregion

        #region Helpers
        private void SendGlobalMessage(string message)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                player.ChatMessage($"<size=18><color=#5557AA>Rust Roams</color></size>\n{message}");
            }
        }

        private static string FormatTime(float duration)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(duration);
            if (timeSpan.TotalSeconds < 1)
                return "0 seconds";

            string formattedTime = "";
            if (timeSpan.Minutes > 0)
                formattedTime += $"<color=#5557AA>{timeSpan.Minutes} minute{(timeSpan.Minutes > 1 ? "s" : "")}</color> ";

            if (timeSpan.Seconds > 0)
                formattedTime += $"<color=#5557AA>{timeSpan.Seconds} second{(timeSpan.Seconds > 1 ? "s" : "")}</color>";

            return formattedTime.Trim();
        }

        private string GetClanTag(BasePlayer? player)
        {
            return (string) Clans?.Call("GetClanOf", player)!;
        }

        private List<string> GetClanMembers(BasePlayer? player)
        {
            return (List<string>) Clans?.Call("GetClanMembers", player?.UserIDString)!;
        }
        #endregion

        #region Components
        public class ClanStatistics
        {
            public int Deaths;
            public int Kills;
            public List<string>? Members = new();
            public Dictionary<string, IndividualStatistics> MemberStats = new();
        }

        public class IndividualStatistics
        {
            public int Deaths;
            public int Kills;
        }

        private class ZoneBehaviour : FacepunchBehaviour
        {
            public Vector3 Position = Vector3.zero;
            public float Radius = 100f;
            private readonly List<SphereEntity> _innerSpheres = new();
            private MapMarkerGenericRadius? _roamMarker;
            private SphereCollider _sphereCollider;
            public float Duration { get; set; }

            private void Start()
            {
                GameObject go = gameObject;
                go.transform.position = Position;
                _sphereCollider = go.AddComponent<SphereCollider>();
                _sphereCollider.transform.position = Position;
                _sphereCollider.radius = Radius;
                _sphereCollider.isTrigger = true;
                _sphereCollider.gameObject.layer = (int) Layer.Reserved1;

                for (int i = 0; i < 7; i++)
                {
                    SphereEntity sphere =
                        (SphereEntity) GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab",
                            Position);
                    sphere.currentRadius = Radius * 2;
                    sphere.lerpSpeed = 0;
                    sphere.enableSaving = false;
                    sphere.Spawn();
                    _innerSpheres.Add(sphere);
                }

                Rigidbody innerRb = _innerSpheres[0].gameObject.AddComponent<Rigidbody>();
                innerRb.useGravity = false;
                innerRb.isKinematic = true;

                _roamMarker =
                    GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", Position) as
                        MapMarkerGenericRadius;
                if (_roamMarker != null)
                {
                    _roamMarker.alpha = 0.33f;
                    _roamMarker.color1 = Color.magenta;
                    _roamMarker.color2 = Color.black;
                    _roamMarker.radius = Radius / 75;
                    _roamMarker.Spawn();
                    _roamMarker.SendUpdate();
                }
            }

            private void Update()
            {
                if (Duration > 0)
                {
                    Duration -= Time.deltaTime;
                }
                else
                {
                    KeyValuePair<string?, ClanStatistics> winningClan = _clanStats.Count > 0
                        ? _clanStats.Aggregate((x, y) => x.Value.Kills > y.Value.Kills ? x : y)
                        : default;

                    if (winningClan.Value.Kills > 0)
                    {
                        StringBuilder message = new();
                        message.AppendLine(
                            $"<color=#5557AA>{winningClan.Key}</color> has won the Roam event with {winningClan.Value.Kills} kills!");
                        message.AppendLine();
                        message.AppendLine($"<color=#5557AA>Members</color> ({winningClan.Value.Members!.Count})");

                        foreach (string userId in winningClan.Value.Members)
                        {
                            IndividualStatistics memberStats = winningClan.Value.MemberStats[userId];
                            BasePlayer? player = BasePlayer.FindAwakeOrSleeping(userId);

                            message.Append(player == null
                                ? $"Unknown Player - K: {memberStats.Kills}, D: {memberStats.Deaths}\n"
                                : $"{player.displayName} - K: {memberStats.Kills}, D: {memberStats.Deaths}\n");
                        }

                        _instance.SendGlobalMessage(message.ToString());
                        Debug.Log($"[Roams] {message}");
                        _clanStats.Clear();
                        _activeRoam.Clear();
                        DestroyImmediate(gameObject);
                        return;
                    }

                    _instance.SendGlobalMessage("There were 0 clans that participated in the Roam event!");
                    Debug.Log("[Roams] There were 0 clans that participated in the Roam event!");
                    _activeRoam.Clear();
                    DestroyImmediate(gameObject);
                }
            }

            private void OnDestroy()
            {
                List<ulong>? list = Pool.GetList<ulong>();
                list.AddRange(_instance._playersInZone);

                foreach (ulong userId in list)
                {
                    BasePlayer? player = BasePlayer.Find(userId.ToString());
                    if (player == null)
                        continue;

                    Timer uiTimer;
                    if (_instance._playerUiTimers.TryGetValue(player.userID, out uiTimer)) uiTimer.Destroy();

                    _instance._playerUiTimers.Remove(player.userID);
                    CuiHelper.DestroyUi(player, "RoamHeaderText");
                    CuiHelper.DestroyUi(player, "TimeRemaining");
                }

                Pool.FreeList(ref list);

                foreach (SphereEntity sphere in _innerSpheres)
                {
                    sphere?.Kill();
                }

                _instance._playersInZone.Clear();

                Destroy(_sphereCollider);
                _roamMarker?.Kill();
            }

            private void OnTriggerEnter(Collider collider)
            {
                if (collider == null)
                    return;

                BasePlayer? player = collider.gameObject.GetComponent<BasePlayer>();
                if (player == null)
                    return;

                _instance._playersInZone.Add(player.userID);

                _instance.RoamUi(player);
                _instance.RoamTimer(player, Duration);
                _instance._playerUiTimers.Add(player.userID,
                    _instance.timer.Every(1f, () => { _instance.RoamTimer(player, Duration); }));
            }

            private void OnTriggerExit(Collider collider)
            {
                if (collider == null)
                    return;

                BasePlayer? player = collider.gameObject.GetComponent<BasePlayer>();
                if (player == null)
                    return;

                _instance._playersInZone.Remove(player.userID);

                Timer uiTimer;
                if (_instance._playerUiTimers.TryGetValue(player.userID, out uiTimer)) uiTimer.Destroy();

                _instance._playerUiTimers.Remove(player.userID);
                CuiHelper.DestroyUi(player, "RoamHeaderText");
                CuiHelper.DestroyUi(player, "TimeRemaining");
            }
        }
        #endregion

        #region UI
        private void RoamUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "RoamHeaderText");
            CuiElementContainer container = new()
            {
                new CuiElement
                {
                    Name = "RoamHeaderText",
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "<color=#5557AA>RUST</color> ROAMS",
                            Font = "robotocondensed-bold.ttf",
                            FontSize = 28,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },
                        new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = "-183.827 -104.972",
                            OffsetMax = "183.827 -65.97"
                        }
                    }
                }
            };

            CuiHelper.AddUi(player, container);
        }

        private void RoamTimer(BasePlayer player, float time)
        {
            CuiHelper.DestroyUi(player, "TimeRemaining");
            CuiElementContainer container = new();

            TimeSpan duration = TimeSpan.FromSeconds(time);
            container.Add(new CuiElement
            {
                Name = "TimeRemaining",
                Parent = "Overlay",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"Time Remaining: <color=#5557AA>{duration.Minutes:D1} minutes</color>",
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },
                    new CuiOutlineComponent {Color = "0 0 0 0.5", Distance = "1 -1"},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",
                        OffsetMin = "-183.827 -137.937",
                        OffsetMax = "184.243 -99.463"
                    }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Hooks
        private void Loaded()
        {
            _instance = this;

            if (Clans == null) Debug.LogError("Clans is not installed! This plugin will not work without it!");
        }

        private void OnServerInitialized()
        {
            foreach (PrefabData prefab in World.Serialization.world.prefabs)
            {
                if (RoamIslands.Count == 2) break;

                if (prefab.category == "Desert Roams" && !RoamIslands.ContainsKey("Desert Roams"))
                    RoamIslands.Add(prefab.category, prefab.position);
                else if (prefab.category == "Snow Roams" && !RoamIslands.ContainsKey("Snow Roams"))
                    RoamIslands.Add(prefab.category, prefab.position);
            }

            timer.Every(RoamInterval, HandleRoamInterval);
        }

        private object OnEntityTakeDamage(BasePlayer victim, HitInfo info)
        {
            BasePlayer? attacker = info?.InitiatorPlayer;
            if (attacker == null) return null;

            if (!_playersInZone.Contains(victim.userID) || !_playersInZone.Contains(attacker.userID)) return null;

            string? attackerClan = GetClanTag(attacker);
            if (string.IsNullOrEmpty(attackerClan)) return null;

            if (!_clanStats.TryGetValue(attackerClan, out ClanStatistics? attackerClanStats))
            {
                List<string>? members = GetClanMembers(attacker);
                if (members.Contains(victim.UserIDString)) return null;

                attackerClanStats = new ClanStatistics
                {
                    Members = members,
                    MemberStats = new Dictionary<string, IndividualStatistics>
                    {
                        [attacker.UserIDString] = new() {Kills = 0, Deaths = 0}
                    },
                    Kills = 0,
                    Deaths = 0
                };

                _clanStats.Add(attackerClan, attackerClanStats);
            }
            else if (attackerClanStats.Members.Contains(victim.UserIDString))
            {
                return null;
            }

            return null;
        }

        private void OnEntityDeath(BasePlayer? victim, HitInfo? info)
        {
            BasePlayer? attacker = info?.InitiatorPlayer;
            if (attacker == null || !attacker.userID.IsSteamId()) return;

            if (!_playersInZone.Contains(victim.userID) || !_playersInZone.Contains(attacker.userID)) return;

            string attackerClan = GetClanTag(attacker);
            if (!string.IsNullOrEmpty(attackerClan))
                if (_clanStats.TryGetValue(attackerClan, out ClanStatistics? attackerClanStats))
                {
                    if (!attackerClanStats.Members.Contains(victim.UserIDString))
                    {
                        attackerClanStats.Kills += 1;
                        attackerClanStats.MemberStats[attacker.UserIDString].Kills += 1;
                    }
                    else
                    {
                        return;
                    }
                }

            string victimClan = GetClanTag(victim);
            if (!string.IsNullOrEmpty(victimClan))
            {
                if (_clanStats.TryGetValue(victimClan, out ClanStatistics? victimClanStats))
                {
                    victimClanStats.Deaths += 1;
                    victimClanStats.MemberStats[victim.UserIDString].Deaths += 1;
                }
                else
                {
                    List<string>? members = GetClanMembers(victim);

                    _clanStats.Add(victimClan,
                        new ClanStatistics
                        {
                            Members = members,
                            MemberStats = new Dictionary<string, IndividualStatistics>
                            {
                                [victim.UserIDString] = new() {Kills = 0, Deaths = 1}
                            },
                            Kills = 0,
                            Deaths = 1
                        });
                }
            }
        }
        #endregion

    }
}