using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Overlays;

/// <summary>
/// This component allows the owner to see the satiation of mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowSatiationIconsComponent : Component
{
    /// <summary>
    /// The <see cref="SatiationTypePrototype"/>s to be shown to the owner of this component.
    /// </summary>
    [DataField]
    public List<ProtoId<SatiationTypePrototype>> ShownTypes = [];
}
