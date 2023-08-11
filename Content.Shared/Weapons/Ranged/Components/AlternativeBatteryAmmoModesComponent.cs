using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AlternativeBatteryAmmoModesComponent : Component
{
    [DataField("batteryAmmoModes")]
    [AutoNetworkedField]
    public List<BatteryAmmoMode> BatteryAmmoModes = new();

    [AutoNetworkedField]
    public int CurrentBatteryAmmoIndex = 1;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed class BatteryAmmoMode
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("fireCost")]
    public float FireCost = 100;
}
