// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.Abilities.Evolution.Components;

[RegisterComponent, NetworkedComponent]

public sealed partial class EvolutionComponent : Component
{
    [DataField("timeUntilEvolution", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan TimeUntilEvolution;

    [DataField("duration")]
    public float Duration = 60f;

    [DataField("spawnedEntities", required: true)]
    public string[] SpawnedEntities { get; set; } = Array.Empty<string>();

    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: false, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SelectEntity = "Error";

    [DataField("evolutionAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? EvolutionAction = "ActionEvolution";

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
