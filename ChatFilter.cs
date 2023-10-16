// Reference: 0Harmony

// WIP

using System;
using System.Collections.Generic;
using ConVar;
using Harmony;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;
using UnityEngine.Networking;

namespace Oxide.Plugins
{
    [Info("Chat Filter", "Strobez", "1.0.0")]
    internal class ChatFilter : RustPlugin
    {
        [PluginReference] private Plugin BetterChatMute;
        
        private const string WebRequestUrl = "https://localhost:7000/server/chatfilters";
        private const string WebApiKey = "";
        private static HarmonyInstance? _harmonyInstance;
        private static string[]? _filteredWords = Array.Empty<string>();
        private static HashSet<ulong> _warnedPlayers = new();

        private static readonly Dictionary<string, string> _headers = new()
        {
            {"X-API-KEY", WebApiKey}
        };

        #region Methods
        private static void GetFilteredWords()
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(WebRequestUrl);
            webRequest.SetRequestHeader("X-API-KEY", WebApiKey);
            webRequest.SendWebRequest().completed += operation =>
            {
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to get chat filter: {webRequest.result} {webRequest.error}");
                    return;
                }

                ChatFilterResponse chatFilterResponse =
                    JsonConvert.DeserializeObject<ChatFilterResponse>(webRequest.downloadHandler.text);

                if (chatFilterResponse.Success && chatFilterResponse.Data != null)
                    _filteredWords = chatFilterResponse.Data;
                else
                    Debug.LogError(
                        $"Failed to get chat filter, reached API but failed to deserialize: {webRequest.downloadHandler.text}");
            };
        }
        #endregion

        #region Hooks
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

        private void OnPlayerDisconnected(BasePlayer player)
        {
            _warnedPlayers.Remove(player.userID);
        }
        #endregion

        #region Harmony Patches
        internal class ChatFilterResponse
        {
            public bool Success { get; set; }
            public string[]? Data { get; set; }
        }

        [HarmonyPatch(typeof(Bootstrap), "StartupShared")]
        internal static class Bootstrap_StartupShared_Patch
        {
            [HarmonyPrefix]
            internal static bool Prefix()
            {
                GetFilteredWords();

                return true;
            }
        }


        [HarmonyPatch(typeof(Chat), "sayImpl")]
        internal class Chat_sayImpl_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ConsoleSystem.Arg arg)
            {
                if (_filteredWords == null || _filteredWords.Length == 0) return true;

                BasePlayer player = arg.Player();
                if (player == null) return true;

                string message = arg.GetString(0, "text");
                if (!IsFiltered(message)) return true;

                player.ChatMessage(
                    "<color=#ff0000>WARNING!</color> Your message contained a filtered word, next time you will be muted.");

                _warnedPlayers.Add(player.userID);
                
                if (IsMuted(player.userID)) return false;
                
                if (_warnedPlayers.Contains(player.userID))
                {
                    MutePlayer(player.userID, "Chat Filter");
                    _warnedPlayers.Remove(player.userID);
                }

                return false;
            }

            private static bool IsFiltered(string message)
            {
                return _filteredWords != null && Array.Exists(_filteredWords, message.Contains);
            }
        }
        #endregion

        #region BetterChatMute Integration
        private static bool MutePlayer(ulong userId, string reason)
        {
            IPlayer player = covalence.Players.FindPlayerById(userId.ToString());
            if (player == null) return false;
        
            BetterChatMute?.Call("API_Mute", player, null, reason, true, false);
            
            return true;
        }
        
        private static bool IsMuted(ulong userId)
        {
            IPlayer player = covalence.Players.FindPlayerById(userId.ToString());
            if (player == null) return false;
            
            bool isMuted = (bool) (BetterChatMute?.Call("API_IsMuted", player) ?? false);
            
            return isMuted;
        }
        #endregion
    }
}