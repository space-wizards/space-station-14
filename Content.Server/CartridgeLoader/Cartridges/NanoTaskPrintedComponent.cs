using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class NanoTaskPrintedComponent : Component
{
    /// <summary>
    /// The task that this item holds
    /// </summary>
    [DataField]
    public NanoTaskItem? Task;
}
