using Content.Shared.Atmos;
using Content.Shared.Medical.Metabolism.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Respiration.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LungsComponent : Component
{
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

    public float TargetVolume
    => Phase switch
    {
        BreathingPhase.Inhale => float.Lerp(NormalInhaleVolume,
            MaxVolume,
            BreathEffort),
        BreathingPhase.Exhale => float.Lerp(MinVolume,
            NormalExhaleVolume,
            1-BreathEffort),
        //We only care about target volume when inhaling or exhaling
        _ => ContainedGas.Volume
    };

    /// <summary>
    /// Can these lungs breathe?
    /// </summary>
    public bool CanBreathe => Phase != BreathingPhase.Suffocating;

    [DataField, AutoNetworkedField]
    public BreathingPhase Phase = BreathingPhase.Pause;

    /// <summary>
    /// The maximum volume that these lungs can expand to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxVolume = 8;

    /// <summary>
    /// The minimum volume that these lungs can compress to. This is bypassed and volume is set to 0 in low pressure environments.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinVolume = 1.5f;

    /// <summary>
    /// Lung volume during a normal inhale
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NormalInhaleVolume = 4f;

    /// <summary>
    /// Lung volume during a normal exhale
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NormalExhaleVolume = 3.5f;

    /// <summary>
    /// How much extra *work* is being put into breathing, this is used to lerp between volume range and it's respective max value
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BreathEffort = 0f;

    /// <summary>
    /// How quickly does breathing effort change based on if we are outside the target range for an absorbed reagent
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EffortSensitivity = 0.05f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public GasMixture ContainedGas = new();

    /// <summary>
    /// What type of respiration does this respirator use
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<MetabolismTypePrototype> MetabolismType;

    /// <summary>
    /// Should we look for our solution on the body or this entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UsesBodySolutions = true;

    /// <summary>
    /// What solutionId to put absorbed reagents into
    /// </summary>
    [DataField, AutoNetworkedField]
    public string TargetSolutionId = "bloodReagents";

    /// <summary>
    /// Cached solution owner entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid SolutionOwnerEntity = EntityUid.Invalid;

    [DataField, AutoNetworkedField]
    public EntityUid CachedTargetSolutionEnt = EntityUid.Invalid;

    /// <summary>
    /// cached data for absorbed gases
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(Gas gas, string reagent, GasMetabolismData)> CachedAbsorbedGasData = new();

    /// <summary>
    /// cached data for waste gases
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(Gas gas, string reagent, GasMetabolismData)> CachedWasteGasData = new();
}


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LungsTickingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPhasedUpdate;

    /// <summary>
    /// Rate that reagents are absorbed from the contained gas, and when low-pressure is checked
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(0.5f);
}


public enum BreathingPhase : byte
{
    Inhale,
    Exhale,
    Pause,
    Hold,
    Suffocating
}
