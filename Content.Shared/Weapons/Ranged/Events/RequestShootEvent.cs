using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on the client to indicate it'd like to shoot.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestShootEvent : EntityEventArgs
{
    public NetEntity Gun;
    public NetCoordinates Coordinates;
    public NetEntity? Target;

    /// <summary>
    /// If true, the gun will attempt to fire a burst. Requires <see cref="GunAltBurstComponent"/>.
    /// </summary>
    public bool AltBurst;
}
