using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Configurations;

/// <summary>
/// Configuration for the Cluwne Beast antag.
/// </summary>
public sealed class CluwneBeastRuleConfiguration : StationEventRuleConfiguration
{
    [DataField("spawncluwnebeast")]
    public int SpawnCluwneBeast = 1;

    [DataField("ghostSpawnPoint", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GhostSpawnPoint = "SpawnPointGhostCluwneBeast";

    [DataField("greetingSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Misc/beast.ogg");
}
