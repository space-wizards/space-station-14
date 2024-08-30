using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

[Serializable, NetSerializable]
public sealed class MapWarpRequest: EntityEventArgs
{
    public readonly NetEntity Uid;
    public readonly Vector2 Target;

    public MapWarpRequest(NetEntity uid, Vector2 target)
    {
        Target = target;
        Uid = uid;
    }
}
