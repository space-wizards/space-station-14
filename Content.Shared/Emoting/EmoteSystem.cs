namespace Content.Shared.Emoting
{
    public sealed class EmoteSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmoteAttemptEvent>(OnEmoteAttempt);
        }

        private void OnEmoteAttempt(EmoteAttemptEvent args)
        {
            if (!TryComp(args.Uid, out SharedEmotingComponent? emote) || !emote.Enabled)
                args.Cancel();
        }
    }
}
