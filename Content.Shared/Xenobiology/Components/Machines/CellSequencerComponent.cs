using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components.Machines;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [ViewVariables]
    public List<Cell> Cells = [];

    [ViewVariables]
    public List<Entity<CellContainerComponent>> CellContainers = [];
}
