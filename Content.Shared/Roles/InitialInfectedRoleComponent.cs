using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

[RegisterComponent, ExclusiveAntagonist, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InitialInfectedRoleComponent : Component, IAntagonistRoleComponent
{
    [DataField, AutoNetworkedField]
    public ProtoId<AntagPrototype>? PrototypeId { get; set; }

    [DataField, AutoNetworkedField]
    public string? Briefing { get; set; }
}
