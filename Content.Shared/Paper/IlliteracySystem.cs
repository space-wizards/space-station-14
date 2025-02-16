namespace Content.Shared.Paper;

/// <summary>
/// A system that prevents those with the IlliterateComponent from writing on paper.
/// Has no effect on reading ability.
/// </summary>
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
        args.Cancelled = true;
    }
}
