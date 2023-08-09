using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Speech
{
    public sealed class SpeechSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<SpeechComponent, ComponentGetState>(OnSpeechGetState);
            SubscribeLocalEvent<SpeechComponent, ComponentHandleState>(OnSpeechHandleState);
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

        private void OnSpeechHandleState(EntityUid uid, SpeechComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not SpeechComponentState state)
                return;

            component.Enabled = state.Enabled;
        }

        private void OnSpeechGetState(EntityUid uid, SpeechComponent component, ref ComponentGetState args)
        {
            args.State = new SpeechComponentState(component.Enabled);
        }

        private void OnSpeakAttempt(SpeakAttemptEvent args)
        {
            if (!TryComp(args.Uid, out SpeechComponent? speech) || !speech.Enabled)
                args.Cancel();
        }

        [Serializable, NetSerializable]
        private sealed class SpeechComponentState : ComponentState
        {
            public readonly bool Enabled;

            public SpeechComponentState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }
}
