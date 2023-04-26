using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Marker component that prevents an entity from using any portals.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class PortalBlacklistComponent : Component { }
