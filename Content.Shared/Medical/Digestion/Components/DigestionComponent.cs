using Content.Shared.Chemistry.Reaction.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Digestion.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Digestion.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DigestionComponent : Component
{
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan UpdateRate = new(0,0,0,1);

    [DataField]
    public TimeSpan LastUpdate;

    public const string ContainedEntitiesContainerId = "digestion-contained-entities";

    public const string DigestingSolutionId = "digesting";

    [DataField(required: true)]
    public List<ProtoId<DigestionTypePrototype>> SupportedDigestionTypes = new();

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
    public ProtoId<ReagentPrototype>? DissolvingReagent = null;

    [DataField]
    public float DissolverConcentration = 0;

    [DataField]
    public ReagentId? CachedDissolverReagent = null;

    [DataField, AutoNetworkedField]
    public float CachedDissolverConc = 0;

    [DataField, AutoNetworkedField]
    public EntityUid CachedDigestionSolution;

    [DataField, AutoNetworkedField]
    public EntityUid CachedAbsorberSolution;

    [DataField]
    public List<RateReaction> CachedDigestionReactions = new();
}
