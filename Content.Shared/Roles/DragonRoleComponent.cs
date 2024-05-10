using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
/// Role used to keep track of space dragons for antag purposes.
/// </summary>
[RegisterComponent, ExclusiveAntagonist, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DragonRoleComponent : Component, IAntagonistRoleComponent
{
    [DataField, AutoNetworkedField]
    public ProtoId<AntagPrototype>? PrototypeId { get; set; }

    [DataField, AutoNetworkedField]
    public string? Briefing { get; set; }
}
