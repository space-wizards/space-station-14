using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LoadoutComponent : Component
{
    /// <summary>
    /// A list of starting gears, of which one will be given, before RoleLoadouts are equipped.
    /// All elements are weighted the same in the list.
    /// </summary>
    [DataField("prototypes")]
    [AutoNetworkedField]
    public List<ProtoId<StartingGearPrototype>>? StartingGear;

    /// <summary>
    /// A list of role loadouts, of which one will be given.
    /// All elements are weighted the same in the list.
    ///
    /// If AddAllRoleLoadout is set to True, all role loadouts will be given.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public List<ProtoId<RoleLoadoutPrototype>>? RoleLoadout;

    /// <summary>
    /// Determines if all RoleLoadouts are added to StartingGear, or just a single random one.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool AddAllRoleLoadout;
}
