using Content.Shared.PlayingCard;
using Content.Shared.Sound;

namespace Content.Server.PlayingCard
{
    [RegisterComponent]
    public sealed class PlayingCardDeckComponent : SharedPlayingCardDeckComponent
    {
        [DataField("shuffleSound")]
        public SoundSpecifier ShuffleSound = new SoundPathSpecifier("/Audio/Effects/cardshuffle.ogg");
    }
}
