using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CellContainerComponent : Component
{
    [ViewVariables]
    public bool Empty => Cells.Count == 0;

    [ViewVariables, AutoNetworkedField]
    public List<Cell> Cells = [];

    [DataField]
    public EntityWhitelist? ToolsTransferWhitelist;
}
