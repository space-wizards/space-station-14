using Content.Shared.Hands.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Events;

/// <summary>
/// Plays a clientside pickup animation by copying the specified entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class StorageAnimationEvent(NetEntity uid) : EntityEventArgs
{
    /// <summary>
    /// Entity to be copied for the clientside animation.
    /// </summary>
    public readonly NetEntity Uid = uid;
}
