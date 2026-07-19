using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Buckle.Components;
using Robust.Shared.Timing;

namespace Content.Server.Buckle.Systems;

public sealed partial class IgniteOnBuckleSystem : EntitySystem
{
    private static readonly EntityTimerId IgniteTimer = new("ignite");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgniteOnBuckleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<IgniteOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);

        SubscribeLocalEvent<ActiveIgniteOnBuckleComponent, MapInitEvent>(ActiveOnInit);
        SubscribeLocalEvent<ActiveIgniteOnBuckleComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnStrapped(Entity<IgniteOnBuckleComponent> ent, ref StrappedEvent args)
    {
        // We cache the values here to the other component.
        // This is done so we have to do less lookups
        var comp = EnsureComp<ActiveIgniteOnBuckleComponent>(args.Buckle);
        comp.FireStacks = ent.Comp.FireStacks;
        comp.MaxFireStacks = ent.Comp.MaxFireStacks;
        comp.IgniteTime = ent.Comp.IgniteTime;
    }

    private void ActiveOnInit(Entity<ActiveIgniteOnBuckleComponent> ent, ref MapInitEvent args)
    {
        // Handle this via a separate MapInit so the component can be added by itself if need be.
        ent.Comp.NextIgniteTime = _timing.CurTime + ent.Comp.NextIgniteTime;
        _timers.SetTimerAt(ent, IgniteTimer, ent.Comp.NextIgniteTime);
        Dirty(ent);
    }

    private void OnUnstrapped(Entity<IgniteOnBuckleComponent> ent, ref UnstrappedEvent args)
    {
        RemCompDeferred<ActiveIgniteOnBuckleComponent>(args.Buckle);
    }

    private void OnTimer(Entity<ActiveIgniteOnBuckleComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != IgniteTimer || !TryComp<FlammableComponent>(ent, out var flammableComponent))
            return;

        var igniteComponent = ent.Comp;
        igniteComponent.NextIgniteTime = args.ScheduledTime + TimeSpan.FromSeconds(igniteComponent.IgniteTime);
        _timers.SetTimerAt(ent, IgniteTimer, igniteComponent.NextIgniteTime);
        Dirty(ent);

        if (flammableComponent.FireStacks > igniteComponent.MaxFireStacks)
            return;

        var stacks = flammableComponent.FireStacks + igniteComponent.FireStacks;
        if (igniteComponent.MaxFireStacks.HasValue)
            stacks = Math.Min(stacks, igniteComponent.MaxFireStacks.Value);

        _flammable.SetFireStacks(ent, stacks, flammableComponent, true);
    }
}
