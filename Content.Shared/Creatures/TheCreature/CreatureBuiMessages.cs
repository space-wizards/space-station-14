using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Creatures.TheCreature;

[Serializable, NetSerializable]
public sealed class CreatureUpgradeMenuBuiState : BoundUserInterfaceState
{
    public readonly float BloodPool;
    public readonly int MaxBloodPool;
    public readonly float BloodConsumedTotal;
    public readonly Dictionary<string, int> UpgradeRanks;

    public CreatureUpgradeMenuBuiState(
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
public sealed partial class CreatureUpgradeMenuActionEvent : InstantActionEvent { }

/// <summary>Sent by the client when the player clicks to evolve the next rank of an upgrade.</summary>
[Serializable, NetSerializable]
public sealed class CreatureEvolveMessage : BoundUserInterfaceMessage
{
    public readonly string UpgradeId;

    public CreatureEvolveMessage(string upgradeId)
    {
        UpgradeId = upgradeId;
    }
}
