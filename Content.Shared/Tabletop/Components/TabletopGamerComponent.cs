using Robust.Shared.GameStates;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// Component for marking an entity as currently playing a tabletop.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTabletopSystem))]
public sealed partial class TabletopGamerComponent : Component
{
    [DataField]
    public EntityUid Tabletop = EntityUid.Invalid;
}
