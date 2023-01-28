using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Emoting
{
    public sealed class EmoteSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<EmotingComponent, ComponentGetState>(OnEmotingGetState);
            SubscribeLocalEvent<EmotingComponent, ComponentHandleState>(OnEmotingHandleState);
        }

        public void SetEmoting(EntityUid uid, bool value, EmotingComponent? component = null)
        {
            if (value && !Resolve(uid, ref component))
                return;

            component = EnsureComp<EmotingComponent>(uid);

            if (component.Enabled == value)
                return;

            Dirty(component);
        }

        private void OnEmotingHandleState(EntityUid uid, EmotingComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not EmotingComponentState state)
                return;

            component.Enabled = state.Enabled;
        }

        private void OnEmotingGetState(EntityUid uid, EmotingComponent component, ref ComponentGetState args)
        {
            args.State = new EmotingComponentState(component.Enabled);
        }

        private void OnEmoteAttempt(EmoteAttemptEvent args)
        {
            if (!TryComp(args.Uid, out EmotingComponent? emote) || !emote.Enabled)
                args.Cancel();
        }

        [Serializable, NetSerializable]
        private sealed class EmotingComponentState : ComponentState
        {
            public bool Enabled { get; }

            public EmotingComponentState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }
}
