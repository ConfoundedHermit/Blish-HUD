using Newtonsoft.Json;

namespace Blish_HUD.Modules {

    public class ModuleContributor {

        [JsonProperty("name")]
        public string Name { get; private set; } = null!;

        [JsonProperty("username")]
        public string Username { get; private set; } = null!;

        [JsonProperty("url")]
        public string Url { get; private set; } = null!;

    }

}
