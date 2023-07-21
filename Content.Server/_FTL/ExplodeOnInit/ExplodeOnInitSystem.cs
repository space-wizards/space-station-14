using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server._FTL.ExplodeOnInit;

/// <summary>
/// This handles exploding on init
/// </summary>
public sealed class ExplodeOnInitSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ExplodeOnInitComponent, ExplosiveComponent>();

        while (query.MoveNext(out var uid, out var explodeOnInitComponent, out var explosiveComponent))
        {
            if (explodeOnInitComponent.ExplodeOnInit || explodeOnInitComponent.TimeUntilDetonation <= 0)
            {
                _explosionSystem.TriggerExplosive(uid, explosiveComponent);
            }
            else
            {
                explodeOnInitComponent.TimeUntilDetonation -= frameTime;
            }
        }
    }
}
