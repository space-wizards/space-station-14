using Content.Shared.PlayingCard;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.PlayingCard
{
    [UsedImplicitly]
    public class PlayingCardSystem : SharedPlayingCardSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlayingCardComponent, StackCountChangedEvent>(OnStackCountChanged);
        }

        private void OnStackCountChanged(EntityUid uid, PlayingCardComponent component, StackCountChangedEvent args)
        {
            // Dirty the UI now that the stack count has changed.
            component.UiUpdateNeeded = true;
        }
    }
}
