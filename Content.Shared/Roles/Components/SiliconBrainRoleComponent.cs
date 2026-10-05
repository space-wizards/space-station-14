using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Used on Silicon's minds to get the appropriate mind role
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SiliconBrainRoleComponent : BaseMindRoleComponent;
