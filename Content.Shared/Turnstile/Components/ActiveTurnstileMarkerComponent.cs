using Robust.Shared.GameStates;

namespace Content.Shared.Turnstile.Components;

/// <summary>
/// This is a marker component used to denote when a Turnstile needs an update this frame.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveTurnstileMarkerComponent : Component
{

}
