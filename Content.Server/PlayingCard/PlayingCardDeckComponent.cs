using Content.Shared.PlayingCard;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server.PlayingCard
{
    [RegisterComponent]
    public sealed class PlayingCardDeckComponent : SharedPlayingCardDeckComponent
    {
        [ViewVariables]
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(PlayingCardDeckUiKey.Key);

        [DataField("playingCardContent")]
        public string PlayingCardContentPrototypeID = string.Empty;
    }
}
