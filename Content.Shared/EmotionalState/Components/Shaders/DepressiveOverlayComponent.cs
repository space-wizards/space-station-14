using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Shader for the depressive state that applies a medium black-and-white effect (Standard object colors can still be distinguished).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DepressiveEmotionOverlayComponent : Component;
