using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Respiration.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Respiration.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RespiratorComponent  : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    /// <summary>
    ///     The interval between updates. CycleTime (Inhale/exhale time) is added on top of this
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How fast does it take to complete an exhale-inhale cycle
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CycleRate = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How much gas is inhaled per cycle in litres
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 InhaleVolume = 0.5;

    [DataField, AutoNetworkedField]
    public SharedGasMixture ContainedGases = new();

    /// <summary>
    /// What type of respiration does this respirator use
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<RespirationType> RespirationType;

    /// <summary>
    /// What solutionId should to put absorbed reagents into
    /// </summary>
    [DataField, AutoNetworkedField]
    public string AbsorbOutputSolution = "Bloodstream";

    /// <summary>
    /// What solutionId should to take waste reagents out of
    /// </summary>
    [DataField, AutoNetworkedField]
    public string WasteSourceSolution = "Bloodstream";

    [DataField, AutoNetworkedField]
    public bool GetSolutionsFromEvent = true;

    /// <summary>
    /// cached data for absorbed gases
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(Gas gas, string? reagent, float maxAbsorption)> CachedAbsorbedGasData = new();

    /// <summary>
    /// cached data for waste gases
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(Gas gas, string? reagent, float maxAbsorption)> CachedWasteGasData = new();

    [DataField, AutoNetworkedField]
    public EntityUid CachedAbsorptionSolutionEnt;

    [DataField, AutoNetworkedField]
    public EntityUid CachedWasteSolutionEnt;
}
