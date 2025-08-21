using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;

namespace Content.Server._Ronstation.Roles;

/// <summary>
/// Added to mind role entities to tag that they are a blood brother.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VampireRoleComponent : BaseMindRoleComponent
{
    [DataField, AutoNetworkedField]
    public EntityUid? Vampire;
}