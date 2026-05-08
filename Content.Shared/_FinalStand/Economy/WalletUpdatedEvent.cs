using Robust.Shared.Serialization;

namespace Content.Shared._FinalStand.Economy;

[Serializable, NetSerializable]
public sealed class WalletUpdatedEvent : EntityEventArgs
{
    public readonly int Credits;
    public readonly int PerkPoints;

    public WalletUpdatedEvent(int credits, int perkPoints)
    {
        Credits = credits;
        PerkPoints = perkPoints;
    }
}
