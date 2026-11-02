using Content.Shared.CF.Character;
using Content.Shared.CF.Shop;

namespace Content.Client.CF.Character;

public enum CFShopCategory
{
    Ammunition,
    Armor,
    Consumable,
    Container,
    Equipment,
    Kit,
    Shield,
    Treasure,
    Weapon,
}

/// <summary>One row in the shop table. Prices are copper: 10 cp = 1 sp, 10 sp = 1 gp, 10 gp = 1 pp.</summary>
public sealed class CFShopListing
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int Level { get; init; }
    public CFRarity Rarity { get; init; } = CFRarity.Common;
    public CFShopCategory Category { get; init; }
    public int CostCopper { get; init; }
    public string[] Traits { get; init; } = Array.Empty<string>();
    public string[] Armor { get; init; } = Array.Empty<string>();

    public static string FormatCost(int copper)
    {
        return CFCoins.FormatPrice(copper);
    }
}
