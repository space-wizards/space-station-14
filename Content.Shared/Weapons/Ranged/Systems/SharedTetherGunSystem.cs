using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract class SharedTetherGunSystem : EntitySystem
{
    public const string CommandName = "tethergun";
}

/// <summary>
/// Sent from server to client if tether gun is toggled on.
/// </summary>
[Serializable, NetSerializable]
public sealed class TetherGunToggleMessage : EntityEventArgs
{
    public bool Enabled;
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

/// <summary>
/// Client can't know the tether's <see cref="EntityUid"/> in advance so needs to be told about it for prediction.
/// </summary>
[Serializable, NetSerializable]
public sealed class PredictTetherEvent : EntityEventArgs
{
    public EntityUid Entity;
}
