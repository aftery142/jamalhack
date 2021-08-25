extern alias sdk;

using sdk::Microsoft.Xna.Framework;
using sdk::Microsoft.Xna.Framework.Graphics;
using sdk::osu.Graphics.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Feature
{
    public static class Overlay
    {
        private static SpriteManager man = null;
        public static void Reset()
        {
            if (man != null) man.Dispose();
            man = null;
        }
        public static void Draw()
        {
            if (man == null)
            {
                man = new SpriteManager(true);
                man.Add(new pText("jamal hack for the osu game\n:) :) :DD xD", 14, new Vector2(2, 2), 100, true, Color.White));
                Utility.Debug("Overlay initialized.");
            }
            if (Miscellaneous.Watermark)
                man.Draw();
        }
    }
}
