using System;
using Robust.Shared.Serialization;

namespace Content.Shared.PlayingCard
{
    [Serializable, NetSerializable]
    public enum PlayingCardHandVisuals : byte
    {
        CardCount,
        CardList
    }
}
