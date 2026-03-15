using Robust.Shared.GameStates;

namespace Content.Shared.DirectionalSigns.Components;

/// <summary>
/// Component that marks a sign for sprite rotation and offset correction when the 'eye' rotates.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DirectionalSignComponent : Component;
