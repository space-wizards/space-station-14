using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

[Serializable, NetSerializable]
public sealed class NavMapWarpRequest: EntityEventArgs
{
    public readonly NetEntity Uid;
    public readonly Vector2 Target;

    public NavMapWarpRequest(NetEntity uid, Vector2 target)
    {
        Target = target;
        Uid = uid;
    }
}
