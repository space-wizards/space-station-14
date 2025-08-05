using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for the Impaired Mobility disability trait.
/// Applies a base movement speed reduction as determined by the SpeedModifier field.
/// Also increases the time it takes to stand up after falling, as determined by the StandUpTimeModifier field.
/// When an entity holds an item with the MobilityAidComponent, the speed penalty is nullified by MobilityAidSystem.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ImpairedMobilityComponent : Component
{
    /// <summary>
    /// The movement speed modifier applied to the player (0.5 is 50% slower)
    /// </summary>
    [DataField("speedModifier")]
    public float SpeedModifier = 0.5f;

    /// <summary>
    /// The doAfter modifier when getting up after falling (1.5f is 50% slower)
    /// </summary>
    [DataField("standUpTimeModifier")]
    public float StandUpTimeModifier = 1.5f;
}
