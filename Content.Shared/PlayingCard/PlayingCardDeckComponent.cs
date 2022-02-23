using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Content.Shared.Sound;


namespace Content.Shared.PlayingCard
{
    [RegisterComponent]
    public sealed class PlayingCardDeckComponent : Component, ISerializationHooks
    {
        [DataField("stackId")]
        public string StackTypeId { get; private set; } = string.Empty;

        [DataField("cardPrototype")]
        public string CardPrototype { get; private set; } = string.Empty;

        [DataField("cardHandPrototype")]
        public string CardHandPrototype { get; private set; } = string.Empty;

        [DataField("cardList")]
        public List<string> CardList = new();

        [DataField("shuffleSound")]
        public SoundSpecifier ShuffleSound = new SoundPathSpecifier("/Audio/Effects/cardshuffle.ogg");
    }
}
