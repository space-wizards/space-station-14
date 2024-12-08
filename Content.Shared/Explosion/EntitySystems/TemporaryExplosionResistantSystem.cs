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

    // Delete timed-out explosion protections.
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

    // Handle GetExplosionResistanceEvent, nullifying the damage.
    private void GetExplosionResistance(Entity<TemporaryExplosionResistantComponent> entity, ref GetExplosionResistanceEvent args)
    {
        args.DamageCoefficient = 0f;
    }

    /// <summary>
    /// Apply temporary explosion resistance to the entity, protecting it from all direct explosion damage.
    /// Internally applies TemporaryExplosionResistantComponent to the entity.
    /// </summary>
    public void ApplyResistance(EntityUid uid, TimeSpan delay)
    {
        EntityManager.AddComponent(uid, new TemporaryExplosionResistantComponent(_timing.CurTime + delay));
    }
}
