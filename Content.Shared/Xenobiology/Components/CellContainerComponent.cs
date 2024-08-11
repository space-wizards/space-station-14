using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CellContainerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public List<Cell> Cells = [];
}
