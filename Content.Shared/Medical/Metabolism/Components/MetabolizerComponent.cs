using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Medical.Metabolism.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Metabolism.Components;

/// <summary>
/// This component is used to implement metabolic reactions on entities (organs/bodyparts)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MetabolizerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<MetabolismTypePrototype> MetabolismType;
    [DataField(required: true)]
    public float BaseMultiplier = 1;

    [DataField, AutoNetworkedField]
    public DamageSpecifier CachedDeprivationDamage = new();

    [DataField, AutoNetworkedField] public bool UsesBodySolution = true;
    [DataField, AutoNetworkedField] public string AbsorbSolutionId = "bloodReagents";
    [DataField, AutoNetworkedField] public string WasteSolutionId = "bloodReagents";

    [DataField, AutoNetworkedField] public EntityUid CachedAbsorbSolutionEnt = EntityUid.Invalid;
    [DataField, AutoNetworkedField] public EntityUid CachedWasteSolutionEnt = EntityUid.Invalid;
    [DataField, AutoNetworkedField] public List<ReagentQuantity> CachedAbsorbedReagents = new();
    [DataField, AutoNetworkedField] public List<ReagentQuantity> CachedWasteReagents = new();
    [DataField, AutoNetworkedField] public ReagentId? CachedEnergyReagent = null;
    [DataField, AutoNetworkedField] public float CachedKCalPerReagent = 0;
    /// <summary>
    ///     The next time that reagents will be metabolized.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1f);
}
