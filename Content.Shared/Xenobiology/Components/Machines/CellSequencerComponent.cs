using Content.Shared.Materials;
using Content.Shared.Xenobiology.Components.Container;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology.Components.Machines;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public ProtoId<MaterialPrototype> RequiredMaterial = "Plasma";

    [ViewVariables]
    public int MaterialAmount;

    [ViewVariables]
    public List<Cell> Cells = [];

    [ViewVariables]
    public List<Entity<CellContainerComponent>> CellContainers = [];
}
