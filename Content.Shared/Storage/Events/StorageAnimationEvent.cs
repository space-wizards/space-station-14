using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Events;

/// <summary>
/// Plays a clientside animation by entity sprite scale modification.
/// </summary>
[Serializable, NetSerializable]
public sealed class StorageAnimationEvent(NetEntity uid, Vector2 scale) : EntityEventArgs
{
    /// <summary>
    /// Entity that will be used for the animation.
    /// </summary>
    public readonly NetEntity Uid = uid;

    /// <summary>
    /// Scale coefficient that will be changed by sprite scale to get animation.
    /// </summary>
    public readonly Vector2 Scale = scale;
}
