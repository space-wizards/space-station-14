using Robust.Shared.GameStates;

namespace Content.Shared.Containers;

/// <summary>
/// This is used for a container that can have entities inserted into it via a
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DragInsertContainerSystem))]
public sealed partial class DragInsertContainerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ContainerId;

    /// <summary>
    /// If true, there will also be verbs for inserting / removing objects from this container.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool UseVerbs = true;

    /// <summary>
    /// The delay in seconds before a drag will be completed.
    /// </summary>
    [DataField]
    public TimeSpan EntryDelay = TimeSpan.Zero;

    /// <summary>
    /// If entry delay isn't zero, this sets whether an entity dragging itself into the container should be delayed.
    /// </summary>
    [DataField]
    public bool DelaySelfEntry = false;
}
