using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged;

public abstract class SharedTetherGunSystem : EntitySystem
{
    public const string CommandName = "tethergun";
}

[Serializable, NetSerializable]
public sealed class StartTetherEvent : EntityEventArgs
{
    public EntityUid Entity;
    public MapCoordinates Coordinates;
}

[Serializable, NetSerializable]
public sealed class StopTetherEvent : EntityEventArgs {}

[Serializable, NetSerializable]
public sealed class TetherMoveEvent : EntityEventArgs
{
    public MapCoordinates Coordinates;
}
