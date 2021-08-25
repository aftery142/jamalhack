extern alias sdk;

using Core.Type;
using sdk::Microsoft.Xna.Framework.Input;
using sdk::osu.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Management
{
    public static class InputSystem
    {
        private static byte[] last_frame_input = null, _ = null;
        public static bool GetState(Keys key) => (last_frame_input[(byte)key] & 128) > 0;
        public static bool IsChanged(Keys key) 
            => (last_frame_input[(byte)key] & 128) != (_[(byte)key] & 128);
        public static void SetState(Keys key, bool state)
            => last_frame_input[(byte)key] =
                       (byte)(state ? last_frame_input[(byte)key] | 128
                           : last_frame_input[(byte)key] & ~128);
        public static Keys GetPlayKey(byte key) => BindingManager.GetPlayKey((PlayKey)key);
        private static List<Keybind> binds = new List<Keybind>();
        public static void Update(byte[] keys)
        {
            _ = last_frame_input;
            last_frame_input = keys;
            if (_ == null) return;
            for (int i = 0; i <= 255; i++)
            {
                if (IsChanged((Keys)i))
                    binds.ForEach((x) =>
                    {
                        if ((byte)x.Key == i)
                            x.on_switch(x, GetState((Keys)i));
                    });
            }
        }
        public static void Add(Keybind bind)
            => binds.Add(bind);
    }
}
