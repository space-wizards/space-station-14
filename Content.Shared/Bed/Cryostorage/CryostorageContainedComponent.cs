using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Bed.Cryostorage;

/// <summary>
/// This is used to track an entity that is currently being held in Cryostorage.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CryostorageContainedComponent : Component
{
    /// <summary>
    /// Whether or not this entity is being stored on another map or is just chilling in a container
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool StoredWhileDisconnected;

    /// <summary>
    /// The time at which the cryostorage grace period ends.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan? GracePeriodEndTime;

    /// <summary>
    /// The cryostorage this entity is 'stored' in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Cryostorage;

    [DataField]
    public NetUserId? UserId;
}
