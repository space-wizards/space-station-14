using Content.Shared.Body.Components;
using Content.Shared.Body.Part;

namespace Content.Shared.Body.Events
{
    // All of these events are raised on a mechanism entity when added/removed to a body in different
    // ways.

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part.
    /// </summary>
    [ByRefEvent]
    public readonly record struct OrganAddedEvent(Entity<BodyPartComponent> Part);

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part within a body.
    /// </summary>
    [ByRefEvent]
    public readonly record struct OrganAddedToBodyEvent(Entity<BodyComponent> Body, Entity<BodyPartComponent> Part);

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part.
    /// </summary>
    [ByRefEvent]
    public readonly record struct OrganRemovedEvent(Entity<BodyPartComponent> OldPart);

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part within a body.
    /// </summary>
    [ByRefEvent]
    public readonly record struct OrganRemovedFromBodyEvent(Entity<BodyComponent> OldBody, Entity<BodyPartComponent> OldPart);
}
