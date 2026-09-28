using Content.Shared.FixedPoint;
using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.BiomassReclaimer;

[RegisterComponent]
public sealed partial class BiomassReclaimerComponent : Component
{
    /// <summary>
    /// Sound played when processing starts or resumes after power loss.
    /// </summary>
    [DataField]
    public SoundSpecifier? StartupSound = new SoundPathSpecifier("/Audio/Machines/reclaimer_startup.ogg");

    /// <summary>
    /// Material produced when a processing job completes.
    /// </summary>
    [DataField]
    public ProtoId<MaterialPrototype> OutputMaterial = "Biomass";

    /// <summary>
    /// The interval between chances to spill blood or throw an item while processing.
    /// </summary>
    [DataField]
    public TimeSpan RandomMessInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Chance to spill blood at each mess interval.
    /// </summary>
    [DataField]
    public float BloodSpillChance = 0.2f;

    /// <summary>
    /// Chance to throw an item at each mess interval.
    /// </summary>
    [DataField]
    public float ItemThrowChance = 0.03f;

    /// <summary>
    /// Minimum item throw direction coordinate, inclusive.
    /// </summary>
    [DataField]
    public int ItemThrowDirectionMin = -30;

    /// <summary>
    /// Maximum item throw direction coordinate, exclusive.
    /// </summary>
    [DataField]
    public int ItemThrowDirectionMax = 30;

    /// <summary>
    /// Minimum item throw speed, inclusive.
    /// </summary>
    [DataField]
    public int ItemThrowMinSpeed = 1;

    /// <summary>
    /// Maximum item throw speed, exclusive.
    /// </summary>
    [DataField]
    public int ItemThrowMaxSpeed = 10;

    /// <summary>
    /// Minimum rejected climber throw direction coordinate, inclusive.
    /// </summary>
    [DataField]
    public int ClimberThrowDirectionMin = -2;

    /// <summary>
    /// Maximum rejected climber throw direction coordinate, exclusive.
    /// </summary>
    [DataField]
    public int ClimberThrowDirectionMax = 2;

    /// <summary>
    /// Throw speed for climbers that cannot be processed.
    /// </summary>
    [DataField]
    public float ClimberThrowSpeed = 0.5f;

    /// <summary>
    /// Jitter amplitude while processing is running.
    /// </summary>
    [DataField]
    public float JitterAmplitude = -10f;

    /// <summary>
    /// Jitter frequency while processing is running.
    /// </summary>
    [DataField]
    public float JitterFrequency = 100f;

    /// <summary>
    /// Volume of the blood solution prepared for spilling during a job.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodSpillVolume = 50;

    /// <summary>
    /// Fractional biomass left over from completed jobs, carried into the next job.
    /// </summary>
    [DataField]
    public float YieldRemainder;

    /// <summary>
    /// How many units of biomass it produces for each unit of mass.
    /// </summary>
    [DataField]
    public float YieldPerUnitMass = 0.4f;

    /// <summary>
    /// How many seconds to take to insert an entity per unit of its mass.
    /// </summary>
    [DataField]
    public float BaseInsertionDelay = 0.1f;

    /// <summary>
    /// How much to multiply biomass yield from botany produce.
    /// </summary>
    [DataField]
    public float ProduceYieldMultiplier = 0.25f;

    /// <summary>
    /// The time it takes to process a mob, per mass.
    /// </summary>
    [DataField]
    public float ProcessingTimePerUnitMass = 0.5f;

    /// <summary>
    /// Will this refuse to gib a living mob?
    /// </summary>
    [DataField]
    public bool SafetyEnabled = true;
}
