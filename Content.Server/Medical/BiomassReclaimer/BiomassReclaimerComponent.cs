namespace Content.Server.Medical.BiomassReclaimer;

[RegisterComponent]
public sealed partial class BiomassReclaimerComponent : Component
{
    /// <summary>
    /// The interval between chances to spill blood or throw an item while processing.
    /// </summary>
    [DataField]
    public TimeSpan RandomMessInterval = TimeSpan.FromSeconds(5);

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
