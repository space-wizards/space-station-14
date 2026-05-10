using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FinalStand.Shop;

public enum WeaponUpgradeType : byte
{
    FireRate,
    AngleMax,
    SpawnItem,
}

[DataDefinition]
public sealed partial class WeaponUpgradeDef
{
    [DataField(required: true)] public string Id = "";
    [DataField] public string Name = "";
    [DataField] public string Description = "";
    [DataField] public int MaxLevel = 5;
    [DataField] public int BaseCost = 100;
    [DataField] public WeaponUpgradeType Type = WeaponUpgradeType.FireRate;
    [DataField] public float ValuePerLevel = 1.0f;
    [DataField] public EntProtoId? SpawnProtoId;
    [DataField] public int SpawnCountPerLevel = 1;
}
