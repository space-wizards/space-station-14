namespace Content.Shared.Identity.Systems;

public partial class SharedIdentitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityBlockerComponent, CanKnowIdentityAttemptEvent>(OnCanKnowIdentity);
    }

    private void OnCanKnowIdentity(EntityUid uid, IdentityBlockerComponent component, CanKnowIdentityAttemptEvent args)
    {
        args.Cancel();
    }
}
