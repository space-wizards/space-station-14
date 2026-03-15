using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Tracks how much blood has been collected from this entity for the Blood Cult ritual pool.
/// Used to prevent farming a single entity indefinitely.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodCollectionTrackerComponent : Component
{
    /// <summary>
    /// Total amount of blood collected from this entity so far.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TotalBloodCollected;

    /// <summary>
    /// Maximum amount of blood that can be collected from a single entity.
    /// Prevents farming a single entity indefinitely.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxBloodPerEntity = 100.0f;
}

