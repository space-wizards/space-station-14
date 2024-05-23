using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Medical.Metabolism.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Metabolism.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MetabolismComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<MetabolismTypePrototype> MetabolismType;

    [DataField(required: true)]
    public float BaseMultiplier = 1;

    /// <summary>
    /// Short-term stored energy in KiloCalories (KCal)
    /// This simulates the body's glycogen storage, and serves as a buffer before long-term storage is used.
    /// if set to -1, it will use the maxFastStorageValue
    /// </summary>
    [DataField("initialCalorieBuffer"), AutoNetworkedField]
    public float CalorieBuffer = -1;

    /// <summary>
    /// Maximum amount of KiloCalories that can be stored in fast storage. Once this is reached, all future KCal will be
    /// stored in longterm storage
    /// </summary>
    [DataField("calorieBuffer", required: true), AutoNetworkedField]
    public float CalorieBufferCap = 1500;

    /// <summary>
    /// Longer term stored energy in KiloCalories (KCal)
    /// This simulates the body's fat/adipose storage, and serves as a long term fall back if all glycogen is used up.
    /// If this is completely used up, metabolism stops and organs start dying
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float CalorieStorage = 80000;

    /// <summary>
    /// Should this fetch the solution from the body or the owner of this component?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UsesBodySolution = true;
    /// <summary>
    /// What is the solution ID we are using?
    /// </summary>
    [DataField, AutoNetworkedField]
    public string AbsorbSolutionId = "bloodReagents";


    [DataField] public float CachedReagentTarget;
    [DataField] public EntityUid CachedSolutionEnt = EntityUid.Invalid;
    [DataField] public ReagentId? CachedEnergyReagent = null;
    [DataField] public float CachedKCalPerReagent;
    [DataField] public DamageSpecifier CachedDeprivationDamage = new();

    //TODO: implement sideeffects/medical conditions for low/high blood glucose and starvation. Also hook up hunger.

    /// <summary>
    ///     The next time that reagents will be metabolized.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1f);
}
