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

        private void OnSpeakAttempt(SpeakAttemptEvent args)
        {
            if (!TryComp(args.Uid, out SharedSpeechComponent? speech) || !speech.Enabled)
                args.Cancel();
        }
    }
}
