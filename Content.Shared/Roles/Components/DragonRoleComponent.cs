using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are a space dragon.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DragonRoleComponent : BaseMindRoleComponent;
