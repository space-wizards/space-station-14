using Content.Shared.CF.Alchemy;
using Content.Shared.Clothing;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Shop;

/// <summary>
/// Pathfinder coins. 10 copper become 1 silver, 10 silver become 1 gold, and 10 gold become 1 platinum.
/// The purse is stored as copper so a purchase can break a higher coin.
/// </summary>
public static class CFCoins
{
    public const int CopperPerSilver = 10;
    public const int CopperPerGold = 100;
    public const int CopperPerPlatinum = 1000;

    /// <summary>Level 0 starts with 5 gp. Level 1 and up start with 15 gp.</summary>
    public static int StartingCopper(int level)
    {
        return (level <= 0 ? 5 : 15) * CopperPerGold;
    }

    public static int Remaining(HumanoidCharacterProfile profile, string? jobId, IPrototypeManager prototypes)
    {
        var left = StartingCopper(profile.CFBuild.Level);
        if (string.IsNullOrEmpty(jobId))
            return left;

        var roleId = LoadoutSystem.GetJobPrototype(jobId);
        if (!profile.Loadouts.TryGetValue(roleId, out var role))
            return left;

        return left - Spent(role, prototypes);
    }

    public static int Spent(RoleLoadout role, IPrototypeManager prototypes)
    {
        var total = 0;
        foreach (var picks in role.SelectedLoadouts.Values)
        {
            foreach (var pick in picks)
            {
                if (!prototypes.TryIndex(pick.Prototype, out var loadout))
                    continue;

                total += LoadoutPrice(prototypes, loadout);
            }
        }

        return total;
    }

    public static int LoadoutPrice(IPrototypeManager prototypes, LoadoutPrototype loadout)
    {
        var id = FirstEntity(loadout);
        if (id == null || !prototypes.TryIndex<EntityPrototype>(id, out var proto))
            return 0;

        return EntityPrice(proto);
    }

    public static int EntityPrice(EntityPrototype proto)
    {
        if (proto.TryGetComponent<CFItemPriceComponent>(out var price))
            return Math.Max(0, price.Copper);

        if (proto.TryGetComponent<CFAlchemicalItemComponent>(out var brew))
            return Math.Max(0, brew.PriceGp * CopperPerGold);

        return 0;
    }

    /// <summary>Item price, lowest coins omitted when they are zero.</summary>
    public static string FormatPrice(int copper)
    {
        if (copper <= 0)
            return "—";

        var pp = copper / CopperPerPlatinum;
        var gp = copper % CopperPerPlatinum / CopperPerGold;
        var sp = copper % CopperPerGold / CopperPerSilver;
        var cp = copper % CopperPerSilver;
        var parts = new List<string>();
        if (pp > 0)
            parts.Add($"{pp} pp");
        if (gp > 0)
            parts.Add($"{gp} gp");
        if (sp > 0)
            parts.Add($"{sp} sp");
        if (cp > 0)
            parts.Add($"{cp} cp");
        return string.Join(", ", parts);
    }

    /// <summary>Always shows all four coins, already rolled up at every 10.</summary>
    public static string FormatWallet(int copper)
    {
        var negative = copper < 0;
        var left = Math.Abs(copper);
        var text = Loc.GetString("cf-shop-wallet",
            ("pp", left / CopperPerPlatinum),
            ("gp", left % CopperPerPlatinum / CopperPerGold),
            ("sp", left % CopperPerGold / CopperPerSilver),
            ("cp", left % CopperPerSilver));
        return negative ? "-" + text : text;
    }

    private static string? FirstEntity(LoadoutPrototype loadout)
    {
        foreach (var held in loadout.Inhand)
        {
            if (!string.IsNullOrEmpty(held.Id))
                return held.Id;
        }

        foreach (var pair in loadout.Equipment)
        {
            if (!string.IsNullOrEmpty(pair.Value.Id))
                return pair.Value.Id;
        }

        return null;
    }
}
