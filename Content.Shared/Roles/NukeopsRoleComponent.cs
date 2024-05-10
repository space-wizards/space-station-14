using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
///     Added to mind entities to tag that they are a nuke operative.
/// </summary>
[RegisterComponent, ExclusiveAntagonist, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NukeopsRoleComponent : Component, IAntagonistRoleComponent
{
    [DataField, AutoNetworkedField]
    public ProtoId<AntagPrototype>? PrototypeId { get; set; }

    [DataField, AutoNetworkedField]
    public string? Briefing { get; set; }
}
