using Robust.Shared.Prototypes;

namespace Content.Shared.Creatures.TheCreature;

/// <summary>Shared constants for the creature upgrade system.</summary>
public static class CreatureUpgradeData
{
    /// <summary>Canonical display order for the upgrade UI. Matches creature_upgrades.yml.</summary>
    public static readonly IReadOnlyList<ProtoId<CreatureUpgradePrototype>> UpgradeOrder =
    [
        "CreatureUpgradePredator",
        "CreatureUpgradeQuickness",
        "CreatureUpgradeShadow",
        "CreatureUpgradeVenom",
        "CreatureUpgradeRavenous",
        "CreatureUpgradePry",
        "CreatureUpgradeIronhide",
    ];
}
