using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// This is used for tracking pieces that are simply "holograms" shown on the tabletop
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TabletopHologramComponent : Component
{
    /// <summary>
    /// The prototype that this hologram is mimicking.
    /// <seealso cref="TabletopItemVisuals.Prototype"/>
    /// </summary>
    [DataField]
    public EntProtoId? LastPrototype;
}
