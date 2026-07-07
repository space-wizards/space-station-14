using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Creatures.SpaceLeech;

/// <summary>Fired when the player activates the "Open Upgrade Menu" action.</summary>
public sealed partial class SpaceLeechUpgradeMenuActionEvent : InstantActionEvent { }

/// <summary>Sent by the client when the player clicks to evolve the next rank of an upgrade.</summary>
[Serializable, NetSerializable]
public sealed class SpaceLeechEvolveMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<SpaceLeechUpgradePrototype> UpgradeId;

    public SpaceLeechEvolveMessage(ProtoId<SpaceLeechUpgradePrototype> upgradeId)
    {
        UpgradeId = upgradeId;
    }
}
