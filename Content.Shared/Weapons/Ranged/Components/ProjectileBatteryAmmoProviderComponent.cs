using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ProjectileBatteryAmmoProviderComponent : BatteryAmmoProviderComponent
{
    [DataField("proto", required: true)]
    public EntProtoId Prototype = default!;
}
