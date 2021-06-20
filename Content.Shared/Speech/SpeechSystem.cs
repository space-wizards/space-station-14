using Robust.Shared.GameObjects;

namespace Content.Shared.Speech
{
    public class SpeechSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
        }

        private void OnSpeakAttempt(SpeakAttemptEvent ev)
        {
            if (!ev.Entity.HasComponent<SharedSpeechComponent>())
            {
                ev.Cancel();
            }
        }
    }
}
