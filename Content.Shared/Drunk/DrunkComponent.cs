using Robust.Shared.GameStates;
namespace Content.Shared.Drunk;

[RegisterComponent, NetworkedComponent]

[Access(typeof(SharedDrunkSystem), Other = AccessPermissions.ReadWrite)]
public sealed partial class DrunkComponent : Component {
    [Access(typeof(SharedDrunkSystem), Other = AccessPermissions.ReadWrite)]
    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentBoozePower;
}

