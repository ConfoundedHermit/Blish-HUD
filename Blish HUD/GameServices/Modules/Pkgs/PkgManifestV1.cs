using Newtonsoft.Json;

namespace Blish_HUD.Modules.Pkgs {
    public class PkgManifestV1 : PkgManifest {

        public override SupportedModulePkgVersion ManifestVersion => SupportedModulePkgVersion.V1;

        [JsonProperty("url")]
        public string Url { get; set; } = null!;

        [JsonProperty("description")]
        public string Description { get; set; } = null!;

    }
}
