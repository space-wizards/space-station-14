using System;
using Robust.Shared.Serialization;

namespace Content.Shared.PlayingCard
{
    [Serializable, NetSerializable]
    public enum PlayingCardVisuals : byte
    {
        FacingUp,
        CardName,
        PlayingCardContentPrototypeID
    }
}
