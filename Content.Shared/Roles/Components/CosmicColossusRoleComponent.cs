using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are an Entropic Colossus.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicColossusRoleComponent : BaseMindRoleComponent;
