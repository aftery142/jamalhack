extern alias sdk;

using Core.Management;
using sdk::Microsoft.Xna.Framework.Input;
using sdk::Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Type
{
    public class Keybind
    {
        [JsonProperty("Key")]
        public Keys Key = Keys.None;
        [JsonProperty("Toggle")]
        public bool Toggle = false;
        [JsonIgnore]
        public Action<Keybind, bool> on_switch;
        public Keybind(Action<Keybind, bool> on_switch, Keys key = Keys.None, bool toggle = false)
        {
            Key = key; Toggle = toggle;
            this.on_switch = on_switch;
            InputSystem.Add(this);
        }
    }
}
