using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Added to someone using a jetpack for movement purposes
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class JetpackUserComponent : Component
{
    public EntityUid Jetpack;
}
