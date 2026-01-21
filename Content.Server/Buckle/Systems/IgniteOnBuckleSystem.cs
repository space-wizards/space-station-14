using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Buckle.Components;
using Robust.Shared.Timing;

namespace Content.Server.Buckle.Systems;

public sealed class IgniteOnBuckleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgniteOnBuckleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<IgniteOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);

        SubscribeLocalEvent<ActiveIgniteOnBuckleComponent, MapInitEvent>(ActiveOnInit);
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
        Dirty(ent);
    }

    private void OnUnstrapped(Entity<IgniteOnBuckleComponent> ent, ref UnstrappedEvent args)
    {
        RemCompDeferred<ActiveIgniteOnBuckleComponent>(args.Buckle);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<ActiveIgniteOnBuckleComponent, FlammableComponent>();
        while (query.MoveNext(out var uid, out var igniteComponent, out var flammableComponent))
        {
            if (curTime < igniteComponent.NextIgniteTime)
                continue;

            igniteComponent.NextIgniteTime += TimeSpan.FromSeconds(igniteComponent.IgniteTime);
            Dirty(uid, igniteComponent);

            if (flammableComponent.FireStacks > igniteComponent.MaxFireStacks)
                continue;

            var stacks = flammableComponent.FireStacks + igniteComponent.FireStacks;
            if (igniteComponent.MaxFireStacks.HasValue)
                stacks = Math.Min(stacks, igniteComponent.MaxFireStacks.Value);

            _flammable.SetFireStacks(uid, stacks, flammableComponent, true);
        }
    }
}
