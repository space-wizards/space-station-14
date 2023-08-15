using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BatteryWeaponFireModesComponent : Component
{
    [DataField("fireModes", required: true)]
    [AutoNetworkedField]
    public List<BatteryWeaponFireMode> FireModes = new();

    [DataField("currentFireMode")]
    [AutoNetworkedField]
    public BatteryWeaponFireMode CurrentFireMode = default!;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed class BatteryWeaponFireMode
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("fireCost")]
    public float FireCost = 100;
}
