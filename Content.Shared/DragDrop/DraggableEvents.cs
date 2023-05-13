namespace Content.Shared.DragDrop;

/// <summary>
/// Raised directed on an entity when attempting to start a drag.
/// </summary>
[ByRefEvent]
public record struct CanDragEvent
{
    /// <summary>
    /// False if we are unable to drag this entity.
    /// </summary>
    public bool Handled;
}

/// <summary>
/// Raised directed on a dragged entity to indicate whether it has interactions with the target entity.
/// </summary>
[ByRefEvent]
public record struct CanDropDraggedEvent(EntityUid User, EntityUid Target)
{
    public readonly EntityUid User = User;
    public readonly EntityUid Target = Target;
    public bool Handled = false;

    /// <summary>
    /// Can we drop the entity onto the target? If the event is not handled then there is no supported interactions.
    /// </summary>
    public bool CanDrop = false;
}

/// <summary>
/// Raised directed on the target entity to indicate whether it has interactions with the dragged entity.
/// </summary>
[ByRefEvent]
public record struct CanDropTargetEvent(EntityUid User, EntityUid Dragged)
{
    public readonly EntityUid User = User;
    public readonly EntityUid Dragged = Dragged;
    public bool Handled = false;

    /// <summary>
    /// <see cref="CanDropDraggedEvent"/>
    /// </summary>
    public bool CanDrop = false;
}

/// <summary>
/// Raised directed on a dragged entity when it is dropped on a target entity.
/// </summary>
[ByRefEvent]
public record struct DragDropDraggedEvent(EntityUid User, EntityUid Target)
{
    public readonly EntityUid User = User;
    public readonly EntityUid Target = Target;
    public bool Handled = false;
}

/// <summary>
/// Raised directed on the target entity when a dragged entity is dragged onto it.
/// </summary>
[ByRefEvent]
public record struct DragDropTargetEvent(EntityUid User, EntityUid Dragged)
{
    public readonly EntityUid User = User;
    public readonly EntityUid Dragged = Dragged;
    public bool Handled = false;
}
