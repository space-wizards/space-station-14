using Robust.Shared.GameObjects;

namespace Content.Shared.Speech
{
    public class SpeechSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedSpeechComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        }

        private void OnSpeakAttempt(EntityUid uid, SharedSpeechComponent component, SpeakAttemptEvent args)
        {
            if (!component.Enabled)
                args.Cancel();
        }
    }
}
