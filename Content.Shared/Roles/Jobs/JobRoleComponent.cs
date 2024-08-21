using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Jobs;

/// <summary>
///     Added to mind entities to hold the data for the player's current job.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class JobRoleComponent : Component
{
    //TODO:ERRANT Should job data be back here, after all? Figure it out later
}
