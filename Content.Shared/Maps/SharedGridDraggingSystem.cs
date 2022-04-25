using Robust.Shared.Serialization;

namespace Content.Shared.Maps;

/// <summary>
/// Helper system to allow you to move grids with a mouse.
/// </summary>
public abstract class SharedGridDraggingSystem : EntitySystem
{
    public const string CommandName = "griddrag";
}


/// <summary>
/// Raised on the client to request a grid move to a specific position.
/// </summary>
[Serializable, NetSerializable]
public sealed class GridDragRequestPosition : EntityEventArgs
{
    public EntityUid Grid;
    public Vector2 WorldPosition;
}

[Serializable, NetSerializable]
public sealed class GridDragVelocityRequest : EntityEventArgs
{
    public EntityUid Grid;
    public Vector2 LinearVelocity;
}
