extern alias sdk;

using sdk::DiscordRPC;
using sdk::Newtonsoft.Json;
using sdk::osu;
using sdk::osu.Configuration;
using sdk::osu.GameModes.Play;
using sdk::osu.Online;
using sdk::osu_common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Feature
{
    public class DiscordPresence
    {
        private static bool _enabled = true;
        [JsonProperty("Enabled")]
        public static bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (GameBase.Discord == null) return;
                GameBase.Discord.Disable();
                GameBase.Discord = new DiscordStatusManager();
                ConfigManager.sDiscordRichPresence.TriggerChange();
            }
        }
        private static string _id = "859355004313010206";
        [JsonProperty("ID")]
        public static string ID
        {
            get { return _id; }
            set {
                if (_id == value) return;
                _id = value;
                if (GameBase.Discord == null) return;
                GameBase.Discord.Disable();
                GameBase.Discord = new DiscordStatusManager();
                ConfigManager.sDiscordRichPresence.TriggerChange();
            }
        }
        [JsonProperty("Details")]
        public static string Details = null;
        [JsonProperty("State")]
        public static string State = null;
        [JsonProperty("Small Image Key")]
        public static string sImgKey = "mode";
        [JsonProperty("Small Image Text")]
        public static string sImgText = null;
        [JsonProperty("Large Image Key")]
        public static string lImgKey = "osu_logo";
        [JsonProperty("Large Image Text")]
        public static string lImgText = null;
        [JsonProperty("Uptime")]
        public static bool Uptime = true;

        private static DateTime pTime = DateTime.UtcNow;
        public static string GetID(string s)
            => !Enabled || string.IsNullOrWhiteSpace(ID) ? s : ID;
        public static RichPresence Update(RichPresence p)
        {
            Utility.Debug("Presence updated.");
            if (Uptime) p.Timestamps = new Timestamps(pTime);

            if (!Enabled) return p;
            p.Assets.SmallImageText = (sImgText != null ? sImgText : p.State + " [" + Player.get_Mode() + "]");
            p.Assets.LargeImageText = (lImgText != null ? lImgText : "my account (nine digits)");
            if (Details != null) p.Details = Details;
            else if (p.Details == null) p.Details = "Playing on " + Utility.GetServer()
                    + " with version [" + General.get_INTERNAL_BUILD_NAME() + "]";
            p.State = (State != null ? State : string.Format(
                LocalisationManager.GetString(OsuString.ChatEngine_PrivateMessageReceived), "BanchoBot"));
            p.Assets.SmallImageKey = sImgKey;
            p.Assets.LargeImageKey = lImgKey;
            return p;
        }
    }
}
