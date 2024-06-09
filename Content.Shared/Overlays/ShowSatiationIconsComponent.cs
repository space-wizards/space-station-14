using Content.Shared.Nutrition;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
///     This component allows you to see the hungriness of mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowSatiationIconsComponent : Component {
    [DataField]
    public List<ProtoId<SatiationTypePrototype>> Satiations = new();
}
