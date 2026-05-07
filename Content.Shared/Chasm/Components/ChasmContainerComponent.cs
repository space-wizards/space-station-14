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
}
