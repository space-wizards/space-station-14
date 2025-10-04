using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// Marks objects that can be painted with the spray painter.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PaintableComponent : Component
{
    /// <summary>
    /// Group of styles this airlock can be painted with, e.g. glass, standard or external.
    /// Set to null to make an entity unpaintable.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PaintableGroupPrototype>? Group;
}
