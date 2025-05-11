using Content.Shared.Examine;
using Content.Shared.SprayPainter.Components;
using Robust.Shared.Timing;

namespace Content.Shared.SprayPainter;

public sealed class PaintedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PaintedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<PaintedComponent> ent, ref ExaminedEvent args)
    {
        // If the paint's dried, it isn't detectable.
        if (_timing.CurTime > ent.Comp.RemoveTime)
            return;

        args.PushText(Loc.GetString("spray-painter-on-examined-painted-message"));
    }
}
