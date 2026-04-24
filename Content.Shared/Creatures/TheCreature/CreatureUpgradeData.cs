namespace Content.Shared.Creatures.TheCreature;

/// <summary>Shared constants for the creature upgrade system.</summary>
public static class CreatureUpgradeData
{
    public const int MaxRank = CreatureUpgradePrototype.MaxRank;

    /// <summary>Canonical display order for the upgrade UI. Matches creature_upgrades.yml.</summary>
    public static readonly IReadOnlyList<string> UpgradeOrder = new[]
    {
        "CreatureUpgradePredator",
        "CreatureUpgradeQuickness",
        "CreatureUpgradeShadow",
        "CreatureUpgradeVenom",
        "CreatureUpgradeRavenous",
        "CreatureUpgradePry",
        "CreatureUpgradeIronhide",
    };
}
