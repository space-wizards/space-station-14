using Content.Shared.PlayingCard;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.PlayingCard
{
    // TODO: Naming and presentation and such could use some improvement.
    [RegisterComponent, Friend(typeof(PlayingCardSystem))]
    [ComponentReference(typeof(SharedPlayingCardComponent))]
    public class PlayingCardComponent : SharedPlayingCardComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ThrowIndividually { get; set; } = false;
    }
}
