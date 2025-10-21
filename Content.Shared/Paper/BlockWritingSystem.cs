namespace Content.Shared.Paper;

/// <summary>
/// A system that prevents those with the IlliterateComponent from writing on paper.
/// Has no effect on reading ability.
/// </summary>
public sealed class BlockWritingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlockWritingComponent, PaperWriteAttemptEvent>(OnPaperWriteAttempt);
    }

    private void OnPaperWriteAttempt(Entity<BlockWritingComponent> entity, ref PaperWriteAttemptEvent args)
    {
        args.FailReason = entity.Comp.FailWriteMessage;
        args.Cancelled = true;
    }
}
