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
    /// If true, the player's mind won't be removed from their body when they are moved into cryosleep
    /// allowing them to rejoin later.
    /// </summary>
    [DataField]
    public bool AllowReEnteringBody;

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
