using Robust.Shared.GameObjects;

namespace Content.Shared.Emoting
{
    public class EmoteSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedEmotingComponent, EmoteAttemptEvent>(OnEmoteAttempt);
        }

        private void OnEmoteAttempt(EntityUid entity, SharedEmotingComponent component, EmoteAttemptEvent ev)
        {
            if (!component.Enabled)
                ev.Cancel();
        }
    }
}
