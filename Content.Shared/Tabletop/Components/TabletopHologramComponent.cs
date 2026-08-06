using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// A component used to track tabletop pieces copied from other entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TabletopHologramComponent : Component
{
    /// <summary>
    /// The prototype that this hologram is mimicking.
    /// <seealso cref="TabletopItemVisuals.Prototype"/>
    /// </summary>
    [DataField]
    public EntProtoId? LastPrototype;

    /// <summary>
    /// The table this piece belongs to.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Table;
}
