extern alias sdk;

using sdk::Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Type
{
    public class ClientSettings
    {
        [JsonProperty("Game Path")]
        public string GamePath = null;
        [JsonProperty("Safe Mode")]
        public bool SafeMode = false;
        [JsonProperty("Allow Game Updates")]
        public bool GameUpdates = true;
        [JsonProperty("No Console")]
        public bool DisableConsole = false;
    }
}
