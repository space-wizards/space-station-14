using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanBatteryAmmoProviderComponent : BatteryAmmoProviderComponent
{
    [DataField("proto", required: true)]
    public EntProtoId HitscanEntityProto;
}
