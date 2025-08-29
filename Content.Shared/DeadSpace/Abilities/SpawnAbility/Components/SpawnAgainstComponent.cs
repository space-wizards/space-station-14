// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Abilities.SpawnAbility.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpawnAgainstComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> SpawnedEntities { get; set; } = [];

    [DataField("proto", required: false)]
    public EntProtoId SelectEntity = "Error";

    [DataField]
    public EntProtoId SpawnAgainstAction = "ActionSpawnAgainst";

    [DataField]
    public EntityUid? SpawnAgainstActionEntity;

    [DataField]
    public float Duration = 5f;

    [DataField]
    public SoundSpecifier? SpawnSound = default;
}
