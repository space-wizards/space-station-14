using Robust.Shared.Serialization;

namespace Content.Shared._FinalStand.Shop;

[Serializable, NetSerializable]
public enum FSShopWeaponUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class FSShopBuyMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class FSShopUpgradeMessage(string upgradeId) : BoundUserInterfaceMessage
{
    public readonly string UpgradeId = upgradeId;
}
