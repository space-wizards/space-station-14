using Robust.Shared.GameStates;

namespace Content.Shared.Roles;

/// <summary>
/// Adds a briefing to the character info menu, does nothing else.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RoleBriefingComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Briefing;
}
