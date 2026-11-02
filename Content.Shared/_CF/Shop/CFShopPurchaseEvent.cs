using Robust.Shared.Serialization;

namespace Content.Shared.CF.Shop;

/// <summary>
/// Client asks the server to spawn the accepted shop lines onto the player.
/// </summary>
[Serializable, NetSerializable]
public sealed class CFShopPurchaseEvent : EntityEventArgs
{
    public List<CFShopPurchaseLine> Lines = new();
}

[Serializable, NetSerializable]
public sealed class CFShopPurchaseLine
{
    public string Prototype = "";
    public int Quantity = 1;
}
