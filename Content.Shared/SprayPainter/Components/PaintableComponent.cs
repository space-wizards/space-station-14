using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SprayPainter.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaintableComponent : Component
{
    /// <summary>
    /// Group of styles this airlock can be painted with, e.g. glass, standard or external.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<PaintableGroupPrototype> Group = string.Empty;
}
