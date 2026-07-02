using Robust.Shared.Prototypes;

namespace Content.Shared.Creatures.SpaceLeech;

/// <summary>Shared constants for the space leech upgrade system.</summary>
public static class SpaceLeechUpgradeData
{
    /// <summary>Canonical display order for the upgrade UI. Matches space_leech_upgrades.yml.</summary>
    public static readonly IReadOnlyList<ProtoId<SpaceLeechUpgradePrototype>> UpgradeOrder =
    [
        "SpaceLeechUpgradePredator",
        "SpaceLeechUpgradeQuickness",
        "SpaceLeechUpgradeShadow",
        "SpaceLeechUpgradeVenom",
        "SpaceLeechUpgradeRavenous",
        "SpaceLeechUpgradePry",
        "SpaceLeechUpgradeIronhide",
    ];
}
