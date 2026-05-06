using Robust.Shared.Containers;

namespace Content.Shared.Chasm.Components;

/// <summary>
/// Makes this chasm store entities when they fall inside.
/// </summary>
[RegisterComponent]
public sealed partial class ChasmContainerComponent : Component
{
    /// <summary>
    /// ID of a container that stores fallen entities.
    /// </summary>
    [DataField(required:true)]
    public string ContainerId;

    /// <summary>
    /// If true, stuns the mobs that fall inside, so they can't do anything themselves.
    /// </summary>
    [DataField]
    public bool DoStun = true;
}
