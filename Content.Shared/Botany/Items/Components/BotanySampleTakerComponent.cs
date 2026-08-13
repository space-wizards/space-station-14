using Content.Shared.Botany.Items.Systems;
using Content.Shared.Destructible.Thresholds;
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
    /// Damage range to deal to the plant when taking a sample.
    /// </summary>
    [DataField]
    public MinMax SampleDamage = new(30, 50);

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
