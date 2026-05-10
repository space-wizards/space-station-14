using Robust.Shared.Prototypes;

namespace Content.Shared._FinalStand.Shop;

[RegisterComponent]
public sealed partial class FSShopWeaponComponent : Component
{
    [DataField(required: true)]
    public EntProtoId WeaponProtoId = default!;

    [DataField]
    public int Price = 500;

    [DataField]
    public List<WeaponUpgradeDef> Upgrades = [];
}
