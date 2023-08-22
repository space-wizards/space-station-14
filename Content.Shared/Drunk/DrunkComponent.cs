using Robust.Shared.GameStates;

namespace Content.Shared.Drunk;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDrunkSystem), Other = AccessPermissions.ReadWriteExecute)]
public sealed class DrunkComponent : Component {
    [Access(typeof(SharedDrunkSystem), Other = AccessPermissions.ReadWriteExecute)]
    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentBoozePower;
}
