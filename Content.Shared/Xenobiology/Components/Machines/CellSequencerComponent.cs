using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components.Machines;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string DishSlot = "dishSlot";

    [ViewVariables]
    public Cell? SelectedCell;
}
