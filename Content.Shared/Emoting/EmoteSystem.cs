using Robust.Shared.GameObjects;

namespace Content.Shared.Emoting
{
    public class EmoteSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmoteAttemptEvent>(OnEmoteAttempt);
        }

        private void OnEmoteAttempt(EmoteAttemptEvent ev)
        {
            if (!ev.Entity.HasComponent<SharedEmotingComponent>())
            {
                ev.Cancel();
            }
        }
    }
}
