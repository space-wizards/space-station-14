using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Bed.Cryostorage;

/// <summary>
/// This is used to track an entity that is currently being held in Cryostorage.
/// </summary>
[RegisterComponent]
public sealed partial class CryostorageContainedComponent : Component
{
    /// <summary>
    /// Whether or not this entity is being stored on another map or is just chilling in a container
    /// </summary>
    [DataField]
    public bool StoredOnMap;

    /// <summary>
    /// The time at which the cryostorage grace period ends.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? GracePeriodEndTime;
}
