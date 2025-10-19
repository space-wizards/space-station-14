using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Used for the Impaired Mobility disability trait.
/// Applies a base movement speed reduction as determined by the SpeedModifier field.
/// Also increases the time it takes to stand up after falling, as determined by the StandUpTimeModifier field.
/// When an entity holds an item with the MobilityAidComponent, the speed penalty is nullified.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImpairedMobilityComponent : Component
{
    /// <summary>
    /// The movement speed modifier applied to the player (0.4 is 40% slower)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.4f;

    /// <summary>
    /// The doAfter modifier when getting up after falling (1.4 is 40% slower)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StandUpTimeModifier = 1.4f;
}
