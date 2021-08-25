extern alias sdk;

using Core.Management;
using Core.Type;
using sdk::Microsoft.Xna.Framework.Input;
using sdk::Newtonsoft.Json;
using sdk::osu;
using sdk::osu.Audio;
using sdk::osu.Configuration;
using sdk::osu.GameModes.Play;
using sdk::osu.GameModes.Play.Rulesets;
using sdk::osu.GameplayElements;
using sdk::osu.GameplayElements.Beatmaps;
using sdk::osu.GameplayElements.Events;
using sdk::osu.GameplayElements.HitObjects;
using sdk::osu.GameplayElements.HitObjects.Osu;
using sdk::osu.GameplayElements.Scoring;
using sdk::osu.Graphics.Notifications;
using sdk::osu.Graphics.Sprites;
using sdk::osu.Input;
using sdk::osu_common;
using sdk::Un4seen.Bass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Feature
{
    public class Modifiers
    {
        private static double _rate = 1;
        [JsonProperty("Rate Changer")]
        public static bool RateChanger = false;
        [JsonProperty("Rate")]
        public static double Rate
        {
            get { return _rate = Math.Max(0.5, Math.Min(3.0, _rate)); }
            set { _rate = value; }
        }
        [JsonProperty("Remove Flashlight")]
        public static bool FL = true;

        public static void Init()
        {
            new Keybind((x, y) =>
            {
                if (RateChanger && y)
                    Rate += 0.05f;//NotificationManager.ShowMessageMassive("Rate: " + (Rate += 0.05f), 500, 0);
            }, Keys.PageUp);
            new Keybind((x, y) =>
            {
                if (RateChanger && y)
                    Rate -= 0.05f;//NotificationManager.ShowMessageMassive("Rate: " + (Rate -= 0.05f), 500, 0);
            }, Keys.PageDown);
        }
        public static double AdjustFrameInterval(double v)
            => RateChanger && Miscellaneous.Circleguard 
            && AudioEngine.AudioTrack != null && AudioEngine.AudioTrack is AudioTrackBass
                ? v / (Rate / (AudioEngine.AudioTrack.get_PlaybackRate() / 100.0)) : v;
        public static void UpdateRate()
        {
            if (AudioEngine.AudioTrack != null)
            {
                if (!(AudioEngine.AudioTrack is AudioTrackBass))
                    return;
                AudioTrackBass audio = (AudioTrackBass)AudioEngine.AudioTrack;
                if ((!RateChanger || Math.Abs(audio.currentAudioFrequency - audio.initialAudioFrequency * Rate) <= 0.001)
                    && (!Miscellaneous.PitchShift || !audio.get_FrequencyLock())) return;
                double bck = audio.playbackRate;
                if (RateChanger) audio.playbackRate = Rate * 100.0;
                if (Miscellaneous.PitchShift) audio.set_FrequencyLock(false);
                else audio.updatePlaybackRate();
                if (RateChanger) audio.playbackRate = bck;
            }
        }
        private static bool CanFL()
            => FL && ModManager.ModStatus.HasFlag(Mods.Flashlight);
        public static void Draw()
        {
            if (!CanFL() || !Utility.CanPlay() || PlayModes.Osu != Player.get_Mode()) return;
            Player.Instance.hitObjectManager.Draw();
            GameBase.spriteManagerCursor.Draw();
        }
    }
}
