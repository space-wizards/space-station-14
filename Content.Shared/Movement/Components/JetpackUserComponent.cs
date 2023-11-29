using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Added to someone using a jetpack for movement purposes
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JetpackUserComponent : Component
{
    [AutoNetworkedField]
    public EntityUid Jetpack;
}
