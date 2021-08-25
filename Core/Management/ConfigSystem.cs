extern alias sdk;

using Core.Feature;
using Core.Type;
using sdk::Microsoft.Xna.Framework.Input;
using sdk::Newtonsoft.Json;
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
                Load("current");
            }, Keys.Home);
        }
        public static bool Load(string f)
        {
            try
            {
                JsonConvert.PopulateObject
                    (File.ReadAllText(Path.Combine("jamal/configs", f)), _inst);
                return true;
            } catch (Exception e)
            {
                Utility.Fail(e);
                return false;
            }
        }
        public static void Save(string f)
        {
            File.WriteAllText(Path.Combine("jamal/configs", f),
                JsonConvert.SerializeObject(_inst, (Formatting)1));
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
        [JsonProperty("Misc")]
        private Miscellaneous misc = new Miscellaneous();
    }
}
