using Content.Shared.IgnitionSource;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Timing;

namespace Content.Server.Trigger.Systems;

/// <summary>
/// Handles igniting when triggered and stopping ignition after the delay.
/// </summary>
/// <seealso cref="FireStackOnTriggerSystem"/>
public sealed partial class IgniteOnTriggerSystem : EntitySystem
{
    private static readonly EntityTimerId IgniteTimer = new("ignite");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedIgnitionSourceSystem _source = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgniteOnTriggerComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<IgniteOnTriggerComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnTimer(Entity<IgniteOnTriggerComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id == IgniteTimer && TryComp<IgnitionSourceComponent>(ent, out var source) && source.Ignited)
            _source.SetIgnited((ent, source), false);
    }

    private void OnTrigger(Entity<IgniteOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        _source.SetIgnited(target.Value);
        ent.Comp.IgnitedUntil = _timing.CurTime + ent.Comp.IgnitedTime;
        _timers.SetTimerAt(ent, IgniteTimer, ent.Comp.IgnitedUntil);
        Dirty(ent);

        args.Handled = true;
    }
}
