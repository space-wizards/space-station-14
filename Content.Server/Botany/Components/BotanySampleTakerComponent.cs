using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

/// <summary>
/// Component for taking a sample of a plant.
/// </summary>
[RegisterComponent]
[DataDefinition]
[Access(typeof(BotanySampleTakerSystem))]
public sealed partial class BotanySampleTakerComponent : Component
{
    /// <summary>
    /// Minimum damage dealt to the plant when taking a sample.
    /// </summary>
    [DataField]
    public int MinSampleDamage = 30;

    /// <summary>
    /// Maximum damage dealt to the plant when taking a sample.
    /// </summary>
    [DataField]
    public int MaxSampleDamage = 50;

    /// <summary>
    /// Minimum growth stage of the plant to take a sample.
    /// </summary>
    [DataField]
    public int MinSampleStage = 1;

    /// <summary>
    /// Probability of the plant being sampled.
    /// </summary>
    [DataField]
    public float SampleProbability = 0.3f;
}
