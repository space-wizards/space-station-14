// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.DeadSpace.Abilities.SpawnAbility.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CustomSpawnPointComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> SpawnedEntities { get; set; } = [];

    [DataField("proto", required: false)]
    public EntProtoId SelectEntity = "Error";

    [DataField]
    public EntProtoId SpawnPointAction = "ActionSpawnPoint";

    [DataField, AutoNetworkedField]
    public EntityUid? SpawnPointActionEntity;

    [DataField]
    public EntProtoId SelectEntityAction = "ActionSelectEntity";

    [DataField, AutoNetworkedField]
    public EntityUid? SelectEntityActionEntity;

    [DataField]
    public float Duration = 5f;

    [DataField]
    public SoundSpecifier? SpawnSound = default;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates Coords { get; set; } = default!;
}
