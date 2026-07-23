using Robust.Shared.GameStates;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// Component for marking an entity as currently playing a tabletop.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTabletopSystem))]
public sealed partial class TabletopGamerComponent : Component
{
    /// <summary>
    /// The tabletop this entity is playing on.
    /// </summary>
    [DataField]
    public EntityUid Tabletop = EntityUid.Invalid;
}
