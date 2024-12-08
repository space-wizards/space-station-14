using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components.Container;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CellContainerComponent : Component
{
    [ViewVariables]
    public bool Empty => Cells.Count == 0;

    /// <summary>
    /// Current contents of the container.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<Cell> Cells = [];

    /// <summary>
    /// Responsible for the ability of the cell to change
    /// the entity with the container when it is inserted,
    /// as well as in the future.
    /// </summary>
    [DataField]
    public bool AllowModifiers = true;

    /// <summary>
    /// Responsible for the ability to inserted cell into the container.
    /// </summary>
    [DataField]
    public bool AllowTransfer = true;

    /// <summary>
    /// Responsible for the ability to get cell from the container.
    /// </summary>
    [DataField]
    public bool AllowCollection = true;

    [DataField]
    public EntityWhitelist? ToolsTransferWhitelist;
}
