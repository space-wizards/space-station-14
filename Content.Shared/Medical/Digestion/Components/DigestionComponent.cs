using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Digestion.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Digestion.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DigestionComponent : Component
{
    public const string ContainedEntitiesContainerId = "digestion-contained-entities";

    public const string DigestingSolutionId = "digesting";

    [DataField(required: true)]
    public List<ProtoId<DigestionTypePrototype>> SupportedDigestionTypes;

    [DataField(required: false)]
    public List<ProtoId<DigestionTypePrototype>>? PassThroughDigestionTypes = null;

    [DataField, AutoNetworkedField]
    public List<EntityUid> ContainedEntities = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxVolume = 1000;

    [DataField]
    public bool UseBodySolution = true;

    [DataField]
    public string AbsorberSolutionId = "bloodReagents";

    [DataField]
    public ReagentId? CachedDissolverReagent = null;

    [DataField, AutoNetworkedField]
    public EntityUid CachedDigestionSolution;

    [DataField, AutoNetworkedField]
    public EntityUid CachedAbsorberSolution;
}
