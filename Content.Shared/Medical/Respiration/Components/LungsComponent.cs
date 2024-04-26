using Content.Shared.Atmos;
using Content.Shared.Medical.Respiration.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Respiration.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LungsComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPhasedUpdate;

    /// <summary>
    /// Rate that reagents are absorbed from the contained gas, and when low-pressure is checked
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The time it takes to perform an inhalation
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan InhaleTime = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// The time it takes to perform an exhalation
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ExhaleTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum time that breath can be held for. This does not work in a vacuum/low pressure,
    /// attempting it will result in a full exhale.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxHoldTime = TimeSpan.FromSeconds(90);


    /// <summary>
    ///     The interval between updates. CycleTime (Inhale/exhale time) is added on top of this
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PauseTime = TimeSpan.FromSeconds(1);

    public TimeSpan NextPhaseDelay =>
        Phase switch
        {
            BreathingPhase.Inhale => InhaleTime,
            BreathingPhase.Exhale => ExhaleTime,
            BreathingPhase.Pause => PauseTime,
            BreathingPhase.Hold => MaxHoldTime,
            BreathingPhase.Suffocating => InhaleTime,
            _ => throw new Exception("Unknown phase of breathing cycle. This should not happen!")
        };

    /// <summary>
    /// Can these lungs breathe?
    /// </summary>
    public bool CanBreathe => Phase != BreathingPhase.Suffocating;

    [DataField, AutoNetworkedField]
    public BreathingPhase Phase = BreathingPhase.Pause;

    /// <summary>
    /// The maximum volume of gas that can be held in these lungs
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TotalVolume = 8;

    /// <summary>
    /// The amount of gas that stays in the lungs after exhaling. This is bypassed in low pressure environments.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ResidualVolume = 1.5f;

    /// <summary>
    /// The target volume for the current cycle state
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TargetLungVolume = 3;

    /// <summary>
    /// The lung volume increase/decrease when inhaling or exhaling
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TidalVolume = 0.5f;

    /// <summary>
    /// What type of respiration does this respirator use
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<BreathingTypePrototype> RespirationType;

    /// <summary>
    /// Should we look for our solution on the body or this entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UsesBodySolutions = true;

    /// <summary>
    /// What solutionId to put absorbed reagents into
    /// </summary>
    [DataField, AutoNetworkedField]
    public string AbsorbOutputSolution = "bloodReagents";

    /// <summary>
    /// What solutionId to take waste reagents out of
    /// </summary>
    [DataField, AutoNetworkedField]
    public string WasteSourceSolution = "bloodReagents";

    /// <summary>
    /// Cached solution owner entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid SolutionOwnerEntity = EntityUid.Invalid;

    [DataField, AutoNetworkedField]
    public EntityUid CachedAbsorptionSolutionEnt = EntityUid.Invalid;

    [DataField, AutoNetworkedField]
    public EntityUid CachedWasteSolutionEnt = EntityUid.Invalid;

    /// <summary>
    /// cached data for absorbed gases
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(Gas gas, string reagent, float maxAbsorption)> CachedAbsorbedGasData = new();

    /// <summary>
    /// cached data for waste gases
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(Gas gas, string reagent, float maxAbsorption)> CachedWasteGasData = new();
}


public enum BreathingPhase : byte
{
    Inhale,
    Exhale,
    Pause,
    Hold,
    Suffocating
}
