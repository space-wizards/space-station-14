using Content.Shared.PlayingCard;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.PlayingCard
{
    [RegisterComponent, Friend(typeof(PlayingCardSystem))]
    [ComponentReference(typeof(SharedPlayingCardComponent))]
    public class PlayingCardHandComponent : SharedMultiplePlayingCardComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ThrowIndividually { get; set; } = false;

        public string RsiPath = "";
    }
}
