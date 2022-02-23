using Content.Shared.PlayingCard;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;


namespace Content.Server.PlayingCard
{
    [RegisterComponent]
    public sealed class PlayingCardHandComponent : SharedPlayingCardHandComponent
    {
        [ViewVariables]
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(PlayingCardHandUiKey.Key);
    }
}
