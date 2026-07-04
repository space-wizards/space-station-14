using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are a Revolutionary.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevolutionaryRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    /// For headrevs, how many people you have converted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public uint ConvertedCount = 0;
}
