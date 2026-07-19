using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.Projectiles;
using Robust.Shared.Timing;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
/// System for handling injecting into an entity while a projectile is embedded.
/// </summary>
public sealed partial class SolutionInjectWhileEmbeddedSystem : EntitySystem
{
    private static readonly EntityTimerId InjectTimer = new("inject");

    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionInjectWhileEmbeddedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SolutionInjectWhileEmbeddedComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(Entity<SolutionInjectWhileEmbeddedComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
        _timers.SetTimerAt(ent, InjectTimer, ent.Comp.NextUpdate);
    }

    private void OnTimer(Entity<SolutionInjectWhileEmbeddedComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != InjectTimer)
            return;

        ent.Comp.NextUpdate = args.ScheduledTime + ent.Comp.UpdateInterval;
        _timers.SetTimerAt(ent, InjectTimer, ent.Comp.NextUpdate);

        if (!TryComp<EmbeddableProjectileComponent>(ent, out var projectile) || projectile.EmbeddedIntoUid is not { } target)
            return;

        var ev = new InjectOverTimeEvent(target);
        RaiseLocalEvent(ent, ref ev);
    }
}
