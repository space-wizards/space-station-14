using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LoadoutComponent : Component
{
    /// <summary>
    /// A list of starting gears, of which one will be given.
    /// All elements are weighted the same in the list.
    ///
    /// If not specified, <see cref="RoleLoadout"/> will be used instead.
    /// </summary>
    [DataField("prototypes")]
    [AutoNetworkedField]
    public List<ProtoId<StartingGearPrototype>>? StartingGear;

    /// <summary>
    /// A list of role loadouts, of which one will be given.
    /// All elements are weighted the same in the list.
    ///
    /// If not specified, <see cref="StartingGear"/> will be used instead.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public List<ProtoId<RoleLoadoutPrototype>>? RoleLoadout;
}
