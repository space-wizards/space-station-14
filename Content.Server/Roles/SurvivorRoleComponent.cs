using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Adds to a mind role ent to tag they're a Survivor
/// </summary>
[RegisterComponent]
public sealed partial class SurvivorRoleComponent : BaseMindRoleComponent;
