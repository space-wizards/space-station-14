using Robust.Shared.GameStates;

namespace Content.Shared.CartridgeLoader.Cartridges;

/// <summary>
///     Component attached to a piece of paper to indicate that it was printed from NanoTask and can be inserted back into it
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NanoTaskPrintedComponent : Component
{
    /// <summary>
    /// The task that this item holds
    /// </summary>
    [DataField, AutoNetworkedField]
    public NanoTaskItem? Task;
}
