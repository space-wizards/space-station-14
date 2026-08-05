using Robust.Shared.GameStates;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// Component for marking an entity as the background of a tabletop game.
/// Useful for pointing.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTabletopSystem))]
public sealed partial class TabletopBackgroundComponent : Component;
