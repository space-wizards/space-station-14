using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are an adventuring party member.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AdventurerRoleComponent : BaseMindRoleComponent;
