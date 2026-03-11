using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on the client gunsystem to signal that the mouse is up and we should let any canceled guns try to fire again
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestGunCancelReleaseEvent : EntityEventArgs
{
    public NetEntity Gun;
}
