using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Creatures.SpaceLeech;

[Serializable, NetSerializable]
public sealed class SpaceLeechUpgradeMenuBuiState : BoundUserInterfaceState
{
    public readonly float BloodPool;
    public readonly int MaxBloodPool;
    public readonly float BloodConsumedTotal;
    public readonly Dictionary<string, int> UpgradeRanks;

    public SpaceLeechUpgradeMenuBuiState(
        float bloodPool,
        int maxBloodPool,
        float bloodConsumedTotal,
        Dictionary<string, int> upgradeRanks)
    {
        BloodPool = bloodPool;
        MaxBloodPool = maxBloodPool;
        BloodConsumedTotal = bloodConsumedTotal;
        UpgradeRanks = upgradeRanks;
    }
}

/// <summary>Fired when the player activates the "Open Upgrade Menu" action.</summary>
public sealed partial class SpaceLeechUpgradeMenuActionEvent : InstantActionEvent { }

/// <summary>Sent by the client when the player clicks to evolve the next rank of an upgrade.</summary>
[Serializable, NetSerializable]
public sealed class SpaceLeechEvolveMessage : BoundUserInterfaceMessage
{
    public readonly string UpgradeId;

    public SpaceLeechEvolveMessage(string upgradeId)
    {
        UpgradeId = upgradeId;
    }
}
