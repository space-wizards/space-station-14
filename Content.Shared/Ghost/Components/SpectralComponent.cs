using Robust.Shared.GameStates;

namespace Content.Shared.Ghost.Components;

/// <summary>
/// Marker component to identify "ghostly" entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpectralComponent : Component { }
