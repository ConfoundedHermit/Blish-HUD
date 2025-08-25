using System.Collections.Generic;
using JsonSubTypes;
using Newtonsoft.Json;

namespace Blish_HUD.Modules.Pkgs {
    [JsonConverter(typeof(JsonSubtypes), "manifest_version")]
    [JsonSubtypes.KnownSubType(typeof(PkgManifestV1), SupportedModulePkgVersion.V1)]
    public abstract class PkgManifest {

        public abstract SupportedModulePkgVersion ManifestVersion { get; }

        // Required attributes

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; private set; } = null!;

        [JsonProperty("namespace", Required = Required.Always)]
        public string Namespace { get; private set; } = null!;

        [JsonProperty("version", Required = Required.Always), JsonConverter(typeof(Content.Serialization.SemVerConverter))]
        public SemVer.Version Version { get; private set; } = null!;
        
        [JsonProperty("contributors", Required = Required.Always)]
        public List<ModuleContributor> Contributors { get; private set; } = null!;

        [JsonProperty("dependencies", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(ModuleDependency.VersionDependenciesConverter))]
        public List<ModuleDependency> Dependencies { get; private set; } = new List<ModuleDependency>(0);

        [JsonProperty("location", Required = Required.Always)]
        public string Location { get; set; } = null!;

        [JsonProperty("hash", Required = Required.Always)]
        public string Hash { get; set; } = null!;

        [JsonProperty("ispreview", Required = Required.Default)]
        public bool IsPreview { get; set; }

    }
}
