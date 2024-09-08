using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Explosion.EntitySystems;

public sealed class TemporaryExplosionResistantSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemporaryExplosionResistantComponent, GetExplosionResistanceEvent>(GetExplosionResistance);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TemporaryExplosionResistantComponent>();
        while (query.MoveNext(out var uid, out var resistant))
        {
            if (resistant.EndsAt < _timing.CurTime)
                EntityManager.RemoveComponentDeferred(uid, resistant);
        }
    }

    private void GetExplosionResistance(Entity<TemporaryExplosionResistantComponent> entity, ref GetExplosionResistanceEvent args)
    {
        args.DamageCoefficient = 0f;
    }

    public void ApplyResistance(EntityUid uid, TimeSpan delay)
    {
        EntityManager.AddComponent(uid, new TemporaryExplosionResistantComponent(_timing.CurTime + delay));
    }
}
