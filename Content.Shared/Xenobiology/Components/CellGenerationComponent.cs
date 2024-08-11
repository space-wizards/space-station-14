using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology.Components;

/// <summary>
/// Contains all the necessary <see cref="CellPrototype"/>
/// that will be introduced into the <see cref="CellContainerComponent"/> at startup.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CellGenerationComponent : Component
{
    [DataField]
    public List<ProtoId<CellPrototype>> Cells = [];
}
