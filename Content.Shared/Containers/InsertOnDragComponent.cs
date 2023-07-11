using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Containers;

/// <summary>
/// Insert into a container after dragging (with a do after)
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(InsertOnDragSystem))]
public sealed class InsertOnDragComponent : Component
{
    /// <summary>
    /// ID of the target container
    /// </summary>
    [DataField("container", required: true)]
    [ViewVariables]
    public string Container = default!;

    /// <summary>
    /// How much time (in seconds) it takes to perform the DoAfter
    /// </summary>
    [DataField("delay")]
    [ViewVariables]
    public float Delay;
}
