﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NitroEmoji.Client
{

    class PartialGuild
    {
        public string id;
        public string name;

        public PartialGuild(string id, string name) {
            this.id = id;
            this.name = name;
        }

    }

    class PartialEmoji
    {
        public PartialGuild guild;
        public string id;
        public string name;
        public bool animated;

        public PartialEmoji(PartialGuild guild, string id, string name, bool animated) {
            this.id = id;
            this.name = name;
            this.animated = animated;
            this.guild = guild;
        }

        public string GetURL() {
            return $"https://cdn.discordapp.com/emojis/{id}.png";
        }

    }

    class DiscordClient
    {
        public string Token;
        public List<PartialGuild> Guilds = new List<PartialGuild>();
        public List<PartialEmoji> Emojis = new List<PartialEmoji>();

        private void HandleError(WebException e, string taskName) {
            var response = e.Response as HttpWebResponse;
            if (response != null) {
                var res = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Debug.WriteLine("{0} failed: {1} => {2}", taskName, (int)response.StatusCode, res);
            }
        }

        public async Task<bool> Login(string email, string pass) {
            WebClient w = new WebClient();

            string payload = $"{{\"email\":\"{email}\",\"password\":\"{pass}\",\"undelete\":false,\"captcha_key\":null,\"login_source\":null,\"gift_code_sku_id\":null}}";
            w.Headers.Set("Content-Type", "application/json");
            try {
                var res = await w.UploadStringTaskAsync("https://discord.com/api/v6/auth/login", payload);
                dynamic data = JObject.Parse(res);
                Token = data.token;
            } catch (WebException e) {
                HandleError(e, "Login");
                return false;
            }
            return true;
        }

        public async Task<bool> GetGuilds() {
            WebClient w = new WebClient();
            w.Headers.Set("Authorization", Token);
            try {
                var res = await w.DownloadStringTaskAsync("https://discord.com/api/v6/users/@me/guilds");
                dynamic data = JArray.Parse(res);
                foreach(dynamic guild in data) {
                    string id = guild.id;
                    string name = guild.name;
                    Guilds.Add(new PartialGuild(id, name));
                }
            } catch (WebException e) {
                HandleError(e, "Guild list");
                return false;
            }
            return true;
        }

        public async Task<bool> LoadEmojis() {
            WebClient w = new WebClient();
            w.Headers.Set("Authorization", Token);
            foreach (PartialGuild guild in Guilds) {
                try {
                    var res = await w.DownloadStringTaskAsync($"https://discord.com/api/v6/guilds/{guild.id}/emojis");
                    dynamic data = JArray.Parse(res);
                    foreach(dynamic emoji in data) {
                        string id = emoji.id;
                        string name = emoji.name;
                        bool animated = emoji.animated;
                        Emojis.Add(new PartialEmoji(guild, id, name, animated));
                    }
                } catch (WebException e) {
                    HandleError(e, "Emoji list");
                    return false;
                }
            }
            return true;
        }


    }
}
