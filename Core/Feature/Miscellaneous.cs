extern alias sdk;

using sdk::Microsoft.Xna.Framework.Input;
using sdk::Newtonsoft.Json;
using sdk::Newtonsoft.Json.Converters;
using sdk::Newtonsoft.Json.Serialization;
using sdk::osu.Audio;
using sdk::osu_common.Bancho.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Feature
{
    public class Miscellaneous
    {
        public enum SpectatorBufferMode
        {
            None, Freeze, Spazz
        }
        [JsonProperty("Watermark")]
        public static bool Watermark = true;
        [JsonProperty("Stream Proof")]
        public static bool StreamProof = true;
        [JsonProperty("Pausing is not Cheating")]
        public static bool PauseDelay = false;
        [JsonProperty("Taikomania Fix")]
        public static bool Taikomania = false;
        [JsonProperty("Extra Skinnables")]
        public static bool Skinny = true;
        [JsonProperty("Music Pitch Shift")]
        public static bool PitchShift = true;
        [JsonProperty("Circleguard Bypass")]
        public static bool Circleguard = true;
        [JsonProperty("Spectator Buffer")]
        public static SpectatorBufferMode SpectatorBuffer = SpectatorBufferMode.Freeze;

        public static bool Apply(bReplayFrame f)
        {
            if (SpectatorBuffer == SpectatorBufferMode.Spazz)
                f.mouseX = f.mouseY = new Random().Next(int.MinValue, int.MaxValue);
            return SpectatorBuffer != SpectatorBufferMode.Freeze;
        }
    }
}
