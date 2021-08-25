extern alias sdk;

using Core.Type;
using sdk::Microsoft.Xna.Framework;
using sdk::Microsoft.Xna.Framework.Graphics;
using sdk::Microsoft.Xna.Framework.Input;
using sdk::Newtonsoft.Json;
using sdk::osu.Constants;
using sdk::osu.GameplayElements.Scoring;
using sdk::osu.Graphics.Sprites;
using sdk::osu_common.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Feature
{
    public class Submissions
    {
        [JsonProperty("Dialog")]
        public static bool Dialog = true;
        [JsonProperty("Patch")]
        public static bool Patch = false;

        private static Score score = null;
        private static SpriteManager man = null;
        public static void Init()
        {
            new Keybind((x, y) =>
            {
                if (!y || score == null) return;
                Utility.Log("Score submit cancelled.");
                if (man != null) man.Dispose();
                score = null; man = null;
            }, Keys.Back);
            new Keybind((x, y) =>
            {
                if (!y || score == null) return;
                BackgroundWorker bg = new BackgroundWorker();
                bg.DoWork += score.submit;
                bg.RunWorkerAsync();
                Utility.Success("Score submit confirmed.");
                if (man != null) man.Dispose();
                score = null; man = null;
            }, Keys.Enter);
        }
        public static bool OnSubmit(Score s)
        {
            if (!Dialog) return true;
            score = s;
            if (man != null) man.Dispose();
            man = new SpriteManager(true);
            man.Add(new pText("u wanna submit or nah", 14, new Vector2(100, 100), 100, true, Color.Cyan));
            return false;
        }
        public static void Draw()
        {
            if (man != null) man.Draw();
        }
        public static void Update(pWebRequest req)
        {
            if (!Patch || !req.get_Url().Contains("selector.php")) return;
            string iv = req.Parameters["iv"], key = Encoding.ASCII.GetString(Secrets.GetScoreSubmissionKey());
            Utility.Debug(req.Parameters.Remove("bml") ? "bml removed." : "bml not found.");
            if (req.Parameters.ContainsKey("fs"))
                req.Parameters["fs"] = Utility.REncrypt("False:False:False:True:False", key, iv);
            else Utility.Debug("fs not found.");
            if (req.Parameters.ContainsKey("i"))
                req.Parameters["i"] = "";
            else Utility.Debug("i not found.");
            Utility.Success("Submission patched.");
        }
    }
}
