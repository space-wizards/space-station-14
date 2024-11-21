using Content.Shared.Xenobiology.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology.Components.Container;

/// <summary>
/// Contains all the necessary <see cref="CellPrototype"/>
/// that will be introduced into the <see cref="CellContainerComponent"/> at startup.
/// </summary>
/// <seealso cref="SharedCellSystem"/>
[RegisterComponent, NetworkedComponent]
public sealed partial class CellGenerationComponent : Component
{
    /// <summary>
    /// A list of all cell prototypes that will
    /// be inserted into the container when the entity is created,
    /// it does nothing afterward.
    /// </summary>
    [DataField]
    public List<ProtoId<CellPrototype>> Cells = [];
}
