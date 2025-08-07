using Content.Shared.Roles;
using Robust.Shared.GameStates;

namespace Content.Shared.Job;

/// <summary>
///     Added to mind role entities to mark them as a job role entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class JobRoleComponent : BaseMindRoleComponent
{

}
