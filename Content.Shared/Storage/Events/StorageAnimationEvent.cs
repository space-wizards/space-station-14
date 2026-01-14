using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Events;

/// <summary>
/// Plays a clientside animation by entity sprite scale modification.
/// </summary>
[Serializable, NetSerializable]
public sealed class StorageAnimationEvent(NetEntity uid) : EntityEventArgs
{
    /// <summary>
    /// Entity that will be used for the animation.
    /// </summary>
    public readonly NetEntity Uid = uid;
}
