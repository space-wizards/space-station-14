using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
///     Added to mind entities to tag that they are a Revolutionary.
/// </summary>
[RegisterComponent, ExclusiveAntagonist, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevolutionaryRoleComponent : Component, IAntagonistRoleComponent
{
    [DataField, AutoNetworkedField]
    public ProtoId<AntagPrototype>? PrototypeId { get; set; }

    [DataField, AutoNetworkedField]
    public string? Briefing { get; set; }

    /// <summary>
    /// For headrevs, how many people you have converted.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint ConvertedCount = 0;
}
