// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Abilities.Evolution.Components;

[RegisterComponent, NetworkedComponent]

public sealed partial class EvolutionComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeUntilEvolution;

    [DataField]
    public float Duration = 60f;

    [DataField(required: true)]
    public List<EntProtoId> SpawnedEntities { get; set; } = [];

    [DataField("proto", required: false)]
    public EntProtoId SelectEntity = "Error";

    [DataField]
    public EntProtoId EvolutionAction = "ActionEvolution";

    [DataField]
    public string GhostRoleName = "Существо";

    [DataField]
    public string GhostRoleDesk = "Описание";

    [DataField("EvolutionActionEntity")]
    public EntityUid? EvolutionActionEntity;

    [DataField]
    public bool CreateGhostRole = false;
}

[ByRefEvent]
public readonly record struct ReadyEvolutionEvent();
