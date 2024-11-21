using Robust.Shared.GameStates;

namespace Content.Shared.Electrocution;

/// <summary>
/// Allow an entity to see the ElectrocutionOverlay showing electrocuted doors.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ElectrocutionOverlayComponent : Component;
