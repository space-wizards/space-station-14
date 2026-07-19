using Content.Shared.Examine;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that activates from time to time on schedule.
/// </summary>
public sealed partial class XATTimerSystem : BaseXATSystem<XATTimerComponent>
{
    private static readonly EntityTimerId ActivationTimer = new("xat-activation");

    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XATTimerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XATTimerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<XATTimerComponent, EntityTimerEvent>(OnTimer);
        XATSubscribeDirectEvent<ExaminedEvent>(OnExamine);
    }

    private void OnHandleState(Entity<XATTimerComponent> ent, ref ComponentHandleState args)
    {
        Schedule(ent);
    }

    private void OnTimer(Entity<XATTimerComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != ActivationTimer)
            return;

        if (TryComp<XenoArtifactNodeComponent>(ent, out var node) &&
            node.Attached is { } artifactUid &&
            TryComp<XenoArtifactComponent>(artifactUid, out var artifact) &&
            CanTrigger((artifactUid, artifact), (ent.Owner, node)))
        {
            Trigger((artifactUid, artifact), (ent.Owner, ent.Comp, node));
        }

        ent.Comp.NextActivation = args.ScheduledTime + GetNextDelay(ent.Comp);
        Dirty(ent);
        Schedule(ent);
    }

    private void OnMapInit(Entity<XATTimerComponent> ent, ref MapInitEvent args)
    {
        var delay = GetNextDelay(ent);
        ent.Comp.NextActivation = Timing.CurTime + delay;
        Dirty(ent);
        Schedule(ent);
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

    private void Schedule(Entity<XATTimerComponent> ent)
    {
        _timers.SetTimerAt(ent, ActivationTimer, ent.Comp.NextActivation);
    }
}
