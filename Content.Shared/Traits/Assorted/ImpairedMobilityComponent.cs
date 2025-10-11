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

    /// <summary>
    /// The last time this entity tripped due to a makeshift mobility aid.
    /// Used to prevent repeated tripping and infinite recursion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? LastTripTime = null;

    /// <summary>
    /// The next time a trip chance roll can occur when using a makeshift mobility aid.
    /// Used to limit rolling to every 2-5 seconds instead of every movement input.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? NextTripRollTime = null;

    /// <summary>
    /// Minimum time between trip chance rolls (in seconds).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinTripRollInterval = 2.0f;

    /// <summary>
    /// Maximum time between trip chance rolls (in seconds).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxTripRollInterval = 5.0f;

    /// <summary>
    /// Cooldown time after tripping before next roll can occur (in seconds).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TripCooldownTime = 10.0f;
}
