extern alias sdk;

using Core.Feature;
using Core.Type;
using sdk::Microsoft.Xna.Framework.Input;
using sdk::Newtonsoft.Json;
using sdk::Newtonsoft.Json.Converters;
using sdk::osu.Graphics.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Management
{
    public class ConfigSystem
    {
        public static void Init()
        {
            new Keybind((x, y) =>
            {
                /*if (y)
                    NotificationManager.ShowMessageMassive(
                        Load("current") ? "Config reloaded." : "Config loading failed.",
                        1000, 0);*/
                if (y) Load("current");
            }, Keys.Home);
        }
        public static bool Load(string f)
        {
            try
            {
                JsonConvert.PopulateObject
                    (File.ReadAllText(Path.Combine("jamal/configs", f)), _inst, GetSerializerSettings());
                Utility.Success("Loaded config: " + f);
                return true;
            } catch (FileNotFoundException) {
                Utility.Log("Config not found: " + f);
                return false;
            } catch (Exception e)
            {
                Utility.Fail("Failed to load config: " + f);
                Utility.Fail(e);
                return false;
            }
        }
        public static void Save(string f)
        {
            File.WriteAllText(Path.Combine("jamal/configs", f),
                JsonConvert.SerializeObject(_inst, (Formatting)1, GetSerializerSettings()));
            Utility.Success("Saved config: " + f);
        }
        private static JsonSerializerSettings GetSerializerSettings()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());
            return settings;
        }
        private static ConfigSystem _inst = new ConfigSystem();

        // tried learning how to play a rhythm game yet?
        //[JsonProperty("Re-replay")]
        //private Rereplay rereplay = new Rereplay();
        //[JsonProperty("Relax")]
        //private Relax relax = new Relax();
        //[JsonProperty("Aim Assist")]
        //private AimAssist aa = new AimAssist();
        //[JsonProperty("Exploit")]
        //private Exploit exploit = new Exploit();

        [JsonProperty("Submission Helper")]
        private Submissions submissions = new Submissions();
        [JsonProperty("Modifiers")]
        private Modifiers modifiers = new Modifiers();
        [JsonProperty("Discord Rich Presence")]
        private DiscordPresence discord = new DiscordPresence();
        [JsonProperty("Misc")]
        private Miscellaneous misc = new Miscellaneous();
    }
}
