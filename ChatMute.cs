// WIP


using System;
using System.Collections.Generic;
using ConVar;

namespace Oxide.Plugins
{
    [Info("ChatMute", "Strobez", "1.0.0")]
    public class ChatMute : RustPlugin
    {

        private Dictionary<int, int> _muteDurations = new()
        {
            {1, 1},
            {2, 7},
            {3, 14},
            {4, 30}
        };

        private Dictionary<string, MuteData> _mutes = new();

        private void OnServerInitialized()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CheckMute(player);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            CheckMute(player);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            CheckMute(player);
        }

        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (channel != Chat.ChatChannel.Global) return null;
            
            if (_mutes.ContainsKey(player.UserIDString))
            {
                MuteData muteData = _mutes[player.UserIDString];
                if (muteData.IsExpired()) _mutes.Remove(player.UserIDString);
                else
                {
                    player.ChatMessage($"You are currently muted until {muteData.Expires}");
                    return true;
                }
            }
            
            return null;
        }

        private void CheckMute(BasePlayer player)
        {
            if (_mutes.ContainsKey(player.UserIDString))
            {
                MuteData muteData = _mutes[player.UserIDString];
                if (muteData.IsExpired()) _mutes.Remove(player.UserIDString);
            }
        }

        [ConsoleCommand("muteid")]
        private void CmdMuteId(ConsoleSystem.Arg arg)
        {
            if (!arg.IsRcon)
                return;

            if (arg.Args == null || arg.Args.Length < 2)
            {
                arg.ReplyWith("Usage: muteid <steamid> <reason>");
                return;
            }

            string userId = arg.Args[0];
            string reason = arg.Args[1];

            MuteData? muteData = _mutes.ContainsKey(userId) ? _mutes[userId] : null;
            if (muteData != null)
            {
                muteData.Count++;
                muteData.Expires = DateTime.UtcNow.AddDays(_muteDurations[muteData.Count]);
            }
            else
            {
                muteData = new MuteData
                {
                    SteamId = userId,
                    Expires = DateTime.UtcNow.AddDays(1),
                    Count = 1
                };
            }

            _mutes[userId] = muteData;

            arg.ReplyWith($"User {userId} was muted until {muteData.Expires} for reason: {reason}");
        }

        public class MuteData
        {
            public int Count;
            public string SteamId { get; set; }
            public DateTime Expires { get; set; }

            public bool IsExpired()
            {
                return DateTime.UtcNow > Expires;
            }
        }
    }
}