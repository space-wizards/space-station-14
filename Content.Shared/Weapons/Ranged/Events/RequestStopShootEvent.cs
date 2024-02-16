using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on the client to request it would like to stop hooting.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestStopShootEvent : EntityEventArgs
{
    public NetEntity Gun;
}
