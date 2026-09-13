using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LoadoutComponent : Component
{
    /// <summary>
    /// If true, will select the same starting gear as the role loadout.
    /// </summary>
    /// <remarks>
    /// Requires starting gear and role loadout counts to be the same.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool SelectSame = false;

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
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public List<ProtoId<RoleLoadoutPrototype>>? RoleLoadout;
}
