using Robust.Shared.Serialization;

namespace Content.Shared.PlayingCard
{
    [RegisterComponent]
    public sealed class PlayingCardComponent : Component, ISerializationHooks
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public string CardDeckID = string.Empty;

        [DataField("cardName")]
        public string CardName = "Playing Card";
        [DataField("isFacingUp")]
        public bool FacingUp = false;
        [DataField("cardHandPrototype")]
        public string CardHandPrototype { get; private set; } = string.Empty;
        [DataField("playingCardPrototype")]
        public string PlayingCardPrototype { get; private set; } = string.Empty;
        [DataField("playingCardContent")]
        public string PlayingCardContentPrototypeID = string.Empty;
    }
}
