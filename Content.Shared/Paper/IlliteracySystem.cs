namespace Content.Shared.Paper;

public sealed class IlliteracySystem: EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IlliterateComponent, PaperWriteAttemptEvent>(OnPaperWriteAttempt);
    }

    private void OnPaperWriteAttempt(Entity<IlliterateComponent> entity, ref PaperWriteAttemptEvent args)
    {
        args.FailReason = entity.Comp.FailWriteMessage;
        args.Cancel();
    }
}
