// Reference: 0Harmony

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using Harmony;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Discord Wipe Announcement", "Strobez", "1.0.0")]
    internal class DiscordWipeAnnouncement : RustPlugin
    {

        #region Configuration
        private static Configuration _config;

        private class Configuration
        {

            [JsonProperty("Color")] public string Color = "#31fffe";

            [JsonProperty("@everyone?")] public bool Everyone = true;

            [JsonProperty("Footer Image URL")] public string FooterImageUrl =
                "https://files.rustoria.co/servers/3x_eu.png";

            [JsonProperty("Image URL")] public string ImageUrl =
                "https://pbs.twimg.com/media/F5gHOVMXcAEEYl_.jpg";

            [JsonProperty("Linking URL")] public string LinkingUrl =
                "";

            [JsonProperty("Store URL")] public string StoreUrl =
                "";

            [JsonProperty("Discord Webhook URL")] public string WebhookUrl =
                "https://discord.com/api/webhooks/1152707554607124610/6gOoiO8og3-GC42rvlEUTaRWFshoBL3gbGbe5-coRuEE-d2mH1sGLqmWWEaP9Ktv-1AH";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("A new configuration file is being generated.");
            _config = new Configuration();
        }


        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
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

        [HarmonyPatch(typeof(SaveRestore), nameof(SaveRestore.Load))]
        internal static class SaveRestore_Load_Patch
        {
            private static WebRequests webrequest = Interface.Oxide.GetLibrary<WebRequests>();

            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo? methodInfo = typeof(SaveRestore_Load_Patch).GetMethod(nameof(SendDiscordMessage));

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode.Equals(OpCodes.Ldstr) && instruction.operand.Equals("OnNewSave"))
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Call, methodInfo);

                        continue;
                    }

                    yield return instruction;
                }
            }

            public static void SendDiscordMessage()
            {
                string ipAddress = $"{ConVar.Server.ip}:{ConVar.Server.port}";

                StringBuilder desc = new();
                desc.AppendLine($"* IP: {ipAddress}");
                desc.AppendLine($"* Map Size: `{ConVar.Server.worldsize}`");
                desc.AppendLine($"* Store: {_config.StoreUrl}");
                desc.AppendLine($"* Link your Accounts: {_config.LinkingUrl}");


                Embed embed = new Embed()
                    .SetTitle(ConVar.Server.hostname)
                    .SetDescription(desc.ToString())
                    .SetImage(_config.ImageUrl)
                    .SetFooter("Wiped:", _config.FooterImageUrl)
                    .SetColor(_config.Color);


                Dictionary<string, string> headers = new Dictionary<string, string> {{"Content-Type", "application/json"}};
                const float timeout = 500f;

                webrequest.Enqueue(_config.WebhookUrl,
                    new DiscordMessage(_config.Everyone ? "@everyone" : "", embed).ToJson(), GetCallback, null,
                    RequestMethod.POST, headers, timeout);
            }

            internal static void GetCallback(int code, string response)
            {
                if (response != null && code == 204) return;

                Debug.LogError($"[DiscordWipeAnnouncement] Error: {code} - Couldn't get an answer from server.");
            }
        }
        #endregion

        #region Discord
        private class DiscordMessage
        {
            public DiscordMessage(string content, params Embed[] embeds)
            {
                Content = content;
                Embeds = embeds.ToList();
            }

            [JsonProperty("content")] public string Content { get; set; }
            [JsonProperty("embeds")] public List<Embed> Embeds { get; set; }


            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        private class Embed
        {
            [JsonProperty("title")] public string Title { get; set; }
            [JsonProperty("description")] public string Description { get; set; }
            [JsonProperty("image")] public Image Image { get; set; }
            [JsonProperty("fields")] public List<Field> Fields { get; set; } = new();
            [JsonProperty("color")] public int Color { get; set; }

            [JsonProperty("footer")] public Footer Footer { get; set; }
            [JsonProperty("timestamp")] public DateTime Timestamp { get; set; } = DateTime.UtcNow;

            public Embed SetImage(string url)
            {
                Image = new Image(url);
                return this;
            }

            public Embed SetTimestamp(DateTime timestamp)
            {
                Timestamp = timestamp;
                return this;
            }

            public Embed SetTitle(string title)
            {
                Title = title;
                return this;
            }

            public Embed SetDescription(string description)
            {
                Description = description;
                return this;
            }

            public Embed SetFooter(string text, string iconUrl)
            {
                Footer = new Footer(text, iconUrl);
                return this;
            }

            public Embed AddField(string name, string value, bool inline)
            {
                Fields.Add(new Field(name, Regex.Replace(value, "<.*?>", string.Empty), inline));
                return this;
            }

            public Embed SetColor(string color)
            {
                string replace = color.Replace("#", "");
                int decValue = int.Parse(replace, NumberStyles.HexNumber);
                Color = decValue;
                return this;
            }
        }

        private class Footer
        {
            public Footer(string text, string iconUrl)
            {
                Text = text;
                IconUrl = iconUrl;
            }

            [JsonProperty("text")] public string Text { get; set; }
            [JsonProperty("icon_url")] public string IconUrl { get; set; }
        }

        private class Image
        {
            public Image(string url)
            {
                Url = url;
            }

            [JsonProperty("url")] public string Url { get; set; }
        }

        private class Field
        {
            public Field(string name, string value, bool inline)
            {
                Name = name;
                Value = value;
                Inline = inline;
            }

            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("value")] public string Value { get; set; }
            [JsonProperty("inline")] public bool Inline { get; set; }
        }
        #endregion

    }
}