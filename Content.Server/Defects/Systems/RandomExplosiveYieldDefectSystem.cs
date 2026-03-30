using Content.Server.Explosion.Components;
using Content.Shared.Defects.Components;
using Content.Shared.Defects.Systems;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server.Defects.Systems;

/// <summary>
/// Randomizes explosive yield and projectile count for grenades with
/// RandomExplosiveYieldDefectComponent at MapInit.
/// Uses public helpers on the owner systems to avoid direct component writes.
/// </summary>
public sealed class RandomExplosiveYieldDefectSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomExplosiveYieldDefectComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(DefectSystem) });
    }

    private void OnMapInit(Entity<RandomExplosiveYieldDefectComponent> ent, ref MapInitEvent args)
    {
        var def = ent.Comp;

        if (TryComp<ExplosiveComponent>(ent.Owner, out var explosive))
        {
            var total = def.TotalIntensityMin.HasValue && def.TotalIntensityMax.HasValue
                ? _random.NextFloat(def.TotalIntensityMin.Value, def.TotalIntensityMax.Value)
                : explosive.TotalIntensity;

            var max = def.MaxIntensityMin.HasValue && def.MaxIntensityMax.HasValue
                ? _random.NextFloat(def.MaxIntensityMin.Value, def.MaxIntensityMax.Value)
                : explosive.MaxIntensity;

            _explosion.SetExplosiveYield((ent.Owner, explosive), total, max);
        }

        if (TryComp<ProjectileGrenadeComponent>(ent.Owner, out var projGrenade))
        {
            if (def.CapacityMin.HasValue && def.CapacityMax.HasValue)
                projGrenade.Capacity = _random.Next(def.CapacityMin.Value, def.CapacityMax.Value + 1);
        }
    }
}
