extern alias sdk;

using sdk::DiscordRPC;
using sdk::osu;
using sdk::osu.GameModes.Play;
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
        static DateTime pTime = DateTime.UtcNow;
        public static RichPresence Update(RichPresence p)
        {
            p.Timestamps = new Timestamps(pTime);
            p.Assets.SmallImageText = p.State + " [" + Player.get_Mode() + "]";
            p.Assets.LargeImageText = "my account (nine digits)";
            if (p.Details == null) p.Details = "Gaming on [" + General.get_INTERNAL_BUILD_NAME() + "]";
            p.State = string.Format(LocalisationManager.GetString(OsuString.ChatEngine_PrivateMessageReceived), "BanchoBot");
            p.Assets.SmallImageKey = "mode";
            return p;
        }
    }
}
