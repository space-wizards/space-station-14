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
    }

    private void OnStrapped(Entity<IgniteOnBuckleComponent> ent, ref StrappedEvent args)
    {
        EnsureComp<IgniteOnBuckleBurningComponent>(ent);
        ent.Comp.NextIgniteTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.IgniteTime);
        Dirty(ent);
    }

    private void OnUnstrapped(Entity<IgniteOnBuckleComponent> ent, ref UnstrappedEvent args)
    {
        RemComp<IgniteOnBuckleBurningComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IgniteOnBuckleBurningComponent, IgniteOnBuckleComponent, StrapComponent>();
        while (query.MoveNext(out var uid, out _, out var igniteComponent, out var strapComponent))
        {
            if (_timing.CurTime < igniteComponent.NextIgniteTime)
                continue;

            igniteComponent.NextIgniteTime += TimeSpan.FromSeconds(igniteComponent.IgniteTime);
            Dirty(uid, igniteComponent);

            if (strapComponent.BuckledEntities.Count == 0)
                continue;

            foreach (var buckledEntity in strapComponent.BuckledEntities)
            {
                if (TryComp<FlammableComponent>(buckledEntity, out var flammable))
                {
                    flammable.FireStacks += igniteComponent.FireStacks;
                    _flammable.Ignite(buckledEntity, uid, flammable);
                }
            }
        }
    }
}
