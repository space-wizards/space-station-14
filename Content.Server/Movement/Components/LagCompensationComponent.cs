using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Movement.Components;

/// <summary>
/// Stores a buffer of previous positions of an entity.
/// Can be used to check the entity's position at a recent point in time, for use with lag-compensation.
/// </summary>
[RegisterComponent]
public sealed partial class LagCompensationComponent : Component
{
    [ViewVariables]
    public readonly Queue<(TimeSpan Time, GameTick Tick, EntityCoordinates Coords, Angle Angle)> Positions = new();
}
