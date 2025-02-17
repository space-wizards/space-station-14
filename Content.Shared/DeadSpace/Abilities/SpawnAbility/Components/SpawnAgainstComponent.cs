// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Abilities.SpawnAbility.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpawnAgainstComponent : Component
{
    [DataField("spawnedEntities", required: true)]
    public string[] SpawnedEntities { get; set; } = Array.Empty<string>();

    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: false, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SelectEntity = "Error";

    [DataField("spawnAgainstAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? SpawnAgainstAction = "ActionSpawnAgainst";

    [DataField("spawnAgainstActionEntity")]
    public EntityUid? SpawnAgainstActionEntity;

    [DataField("duration")]
    public float Duration = 5f;

    [DataField("spawnSound")]
    public SoundSpecifier? SpawnSound = default;
}
