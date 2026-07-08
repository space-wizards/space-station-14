using Content.Shared.Botany.Items.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Items.Components;

/// <summary>
/// Component for items that can function as a plant sample taker.
/// </summary>
[RegisterComponent, NetworkedComponent]
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
