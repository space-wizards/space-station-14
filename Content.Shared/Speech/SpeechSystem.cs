using Robust.Shared.Timing;

namespace Content.Shared.Speech
{
    public sealed class SpeechSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
        }

        public void SetSpeech(EntityUid uid, bool value, SpeechComponent? component = null)
        {
            if (value && !Resolve(uid, ref component))
                return;

            component = EnsureComp<SpeechComponent>(uid);

            if (component.Enabled == value)
                return;

            Dirty(component);
        }

        private void OnSpeakAttempt(SpeakAttemptEvent args)
        {
            if (!TryComp(args.Uid, out SpeechComponent? speech) || !speech.Enabled)
            {
                args.Cancel();
                return;
            }

            var currentTime = _gameTiming.CurTime;

            // Ensure more than the cooldown time has passed since last speaking
            if (currentTime - speech.LastTimeSpoke < speech.SpeechCooldownTime)
            {
                args.Cancel();
                return;
            }

            speech.LastTimeSpoke = currentTime;
        }
    }
}
