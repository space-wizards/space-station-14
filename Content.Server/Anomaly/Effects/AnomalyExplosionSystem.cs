using Content.Server.Atmos.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="AnomalyExplosionComponent"/>
/// </summary>
public sealed class AnomalyExplosionSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _boom = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyExplosionComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnSupercritical(EntityUid uid, AnomalyExplosionComponent component, ref AnomalySupercriticalEvent args)
    {
        _boom.QueueExplosion(
            uid,
            component.ExplosionPrototype,
            component.TotalIntensity,
            component.Dropoff,
            component.MaxTileIntensity
        );
    }
}
