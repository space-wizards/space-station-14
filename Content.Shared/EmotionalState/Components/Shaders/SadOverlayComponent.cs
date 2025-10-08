using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Shader for the sad state that applies a weak black-and-white effect (Standard object colors can be distinguished without much effort).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SadEmotionOverlayComponent : Component;
