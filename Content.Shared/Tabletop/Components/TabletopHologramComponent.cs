using Robust.Shared.GameStates;

namespace Content.Shared.Tabletop.Components;

/// <summary>
/// This is used for tracking pieces that are simply "holograms" shown on the tabletop
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TabletopHologramComponent : Component;
