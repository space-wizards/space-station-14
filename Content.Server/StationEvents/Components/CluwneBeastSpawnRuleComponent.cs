using Content.Server.StationEvents.Events;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Configuration for the Cluwne Beast antag.
/// </summary>
[RegisterComponent, Access(typeof(CluwneBeastSpawnRule))]
public sealed class CluwneBeastSpawnRuleComponent : Component
{
    [DataField("spawncluwnebeast")]
    public int SpawnCluwneBeast = 1;

    [DataField("ghostSpawnPoint", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GhostSpawnPoint = "SpawnPointGhostCluwneBeast";

    [DataField("greetingSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Misc/beast.ogg");
}
