using Content.Shared.Configurable;
using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Configurable;

[RegisterComponent]
[ComponentReference(typeof(SharedConfigurationComponent))]
public sealed class ConfigurationComponent : SharedConfigurationComponent
{
    [DataField("config")]
    public readonly Dictionary<string, string> Config = new();

    [DataField("qualityNeeded", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string QualityNeeded = "Pulsing";
}
