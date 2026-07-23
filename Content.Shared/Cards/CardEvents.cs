using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cards;

[Serializable, NetSerializable]
public sealed class TakeCardEvent : EntityEventArgs
{
    public readonly NetEntity Cards;
    public readonly NetEntity User;
    public readonly int CardInx;

    public TakeCardEvent(NetEntity cards, NetEntity user, int cardInx)
    {
        Cards = cards;
        User = user;
        CardInx = cardInx;
    }
}

[Serializable, NetSerializable]
public sealed class FlipCardsEvent : EntityEventArgs
{
    public readonly NetEntity Cards;

    public FlipCardsEvent(NetEntity cards)
    {
        Cards = cards;
    }
}

[Serializable, NetSerializable]
public sealed class FanCardsEvent : EntityEventArgs
{
    public readonly NetEntity Cards;

    public FanCardsEvent(NetEntity cards)
    {
        Cards = cards;
    }
}

[Serializable, NetSerializable]
public sealed class ShuffleCardsEvent : EntityEventArgs
{
    public readonly NetEntity Cards;

    public ShuffleCardsEvent(NetEntity cards)
    {
        Cards = cards;
    }
}
