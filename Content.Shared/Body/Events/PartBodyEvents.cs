using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Events;

/// <summary>
///     Raised on a body part when added to a body.
/// </summary>
public class PartAddedToBodyEvent : EntityEventArgs
{
    public SharedBodyComponent? OldBody;
    public SharedBodyComponent NewBody;

    public PartAddedToBodyEvent(SharedBodyComponent? oldBody, SharedBodyComponent newBody)
    {
        OldBody = oldBody;
        NewBody = newBody;
    }
}

/// <summary>
///     Raised on a body part when removed from a body.
/// </summary>
public class PartRemovedFromBodyEvent : EntityEventArgs
{
    public SharedBodyComponent OldBody;
    public SharedBodyComponent? NewBody;

    public PartRemovedFromBodyEvent(SharedBodyComponent oldBody, SharedBodyComponent? newBody)
    {
        OldBody = oldBody;
        NewBody = newBody;
    }
}
