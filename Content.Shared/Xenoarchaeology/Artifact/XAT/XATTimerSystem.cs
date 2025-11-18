using Content.Shared.Examine;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that activates from time to time on schedule.
/// </summary>
public sealed class XATTimerSystem : BaseQueryUpdateXATSystem<XATTimerComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XATTimerComponent, MapInitEvent>(OnMapInit);
        XATSubscribeDirectEvent<ExaminedEvent>(OnExamine);
    }

    // We handle the timer resetting here because we need to keep it updated even if the node isn't able to unlock.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var timerQuery = EntityQueryEnumerator<XATTimerComponent>();
        while (timerQuery.MoveNext(out var uid, out var timer))
        {
            if (Timing.CurTime < timer.NextActivation)
                continue;
            timer.NextActivation += GetNextDelay(timer);
            Dirty(uid, timer);
        }
    }

    /// <inheritdoc />
    protected override void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<XATTimerComponent, XenoArtifactNodeComponent> node, float frameTime)
    {
        if (Timing.CurTime > node.Comp1.NextActivation)
            Trigger(artifact, node);
    }

    private void OnMapInit(Entity<XATTimerComponent> ent, ref MapInitEvent args)
    {
        var delay = GetNextDelay(ent);
        ent.Comp.NextActivation = Timing.CurTime + delay;
        Dirty(ent);
    }

    private void OnExamine(Entity<XenoArtifactComponent> artifact, Entity<XATTimerComponent, XenoArtifactNodeComponent> node, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(
            Loc.GetString("xenoarch-trigger-examine-timer",
            ("time", MathF.Ceiling((float) (node.Comp1.NextActivation - Timing.CurTime).TotalSeconds)))
        );
    }

    private TimeSpan GetNextDelay(XATTimerComponent comp)
    {
        return TimeSpan.FromSeconds(comp.PossibleDelayInSeconds.Next(_robustRandom));
    }
}
