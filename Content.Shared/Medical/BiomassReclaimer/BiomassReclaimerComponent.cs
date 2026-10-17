using Content.Shared.FixedPoint;
using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.BiomassReclaimer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BiomassReclaimerSystem))]
public sealed partial class BiomassReclaimerComponent : Component
{
    /// <summary>
    /// The sound played when processing starts or resumes after a power loss.
    /// </summary>
    [DataField]
    public SoundSpecifier? StartupSound = new SoundPathSpecifier("/Audio/Machines/reclaimer_startup.ogg");

    /// <summary>
    /// The material produced when processing an entity.
    /// </summary>
    [DataField]
    public ProtoId<MaterialPrototype> OutputMaterial = "Biomass";

    /// <summary>
    /// The interval between attempts to spill blood or throw an item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RandomMessInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The chance to spill blood at each mess interval.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BloodSpillChance = 0.2f;

    /// <summary>
    /// The chance to throw an item at each mess interval.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ItemThrowChance = 0.03f;

    /// <summary>
    /// The minimum speed of thrown items, inclusive.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ItemThrowMinSpeed = 1f;

    /// <summary>
    /// The maximum speed of thrown items, exclusive.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ItemThrowMaxSpeed = 10f;

    /// <summary>
    /// The jitter amplitude while processing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JitterAmplitude = -10f;

    /// <summary>
    /// The jitter frequency while processing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JitterFrequency = 100f;

    /// <summary>
    /// How much blood can be spilled while processing a mob.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodSpillVolume = 50;

    /// <summary>
    /// Non-integer biomass left over from processing, added to the next yield.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float YieldRemainder;

    /// <summary>
    /// How many units of biomass it produces for each unit of mass.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float YieldPerUnitMass = 0.4f;

    /// <summary>
    /// How many seconds to take to insert an entity per unit of its mass.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseInsertionDelay = 0.1f;

    /// <summary>
    /// How much to multiply biomass yield from botany produce.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ProduceYieldMultiplier = 0.25f;

    /// <summary>
    /// The time it takes to process a mob, per mass.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ProcessingTimePerUnitMass = 0.5f;

    /// <summary>
    /// Will this refuse to gib a living mob?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SafetyEnabled = true;
}
