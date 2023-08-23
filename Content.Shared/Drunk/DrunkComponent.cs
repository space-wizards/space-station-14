using Robust.Shared.GameStates;

namespace Content.Shared.Drunk;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDrunkSystem))]
public sealed partial class DrunkComponent : Component {
    [Access(typeof(SharedDrunkSystem), Other = AccessPermissions.ReadWrite)]
    [ViewVariables]
    public float CurrentBoozePower;
}

