using Robust.Shared.GameStates;

namespace Content.Shared.Roles;

/// <summary>
///     Added to mind entities to tag that they are a Revolutionary.
/// </summary>
[RegisterComponent, ExclusiveAntagonist, NetworkedComponent]
public sealed partial class RevolutionaryRoleComponent : AntagonistRoleComponent
{
    /// <summary>
    /// For headrevs, how many people you have converted.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint ConvertedCount = 0;
}
