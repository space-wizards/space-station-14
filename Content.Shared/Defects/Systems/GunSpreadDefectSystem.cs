using Content.Shared.Defects.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Shared.Defects.Systems;

/// <summary>
/// Applies randomised spread to guns with GunSpreadDefectComponent.
/// Angle deltas are computed at MapInit (sampled absolute - base) and applied
/// via GunRefreshModifiersEvent, so they compose correctly with wield bonuses
/// without needing direct GunComponent write access.
/// </summary>
public sealed class GunSpreadDefectSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunSpreadDefectComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(DefectSystem) });
        SubscribeLocalEvent<GunSpreadDefectComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
    }

    private void OnMapInit(Entity<GunSpreadDefectComponent> ent, ref MapInitEvent args)
    {
        var def = ent.Comp;

        if (TryComp<GunComponent>(ent.Owner, out var gun))
        {
            // Compute angle deltas: (sampled absolute target) - (base yaml angle).
            // Stored on the component so OnRefreshModifiers can add them each time
            // RefreshModifiers is called (e.g. on wield/unwield).
            if (def.MinAngleMin.HasValue && def.MinAngleMax.HasValue)
            {
                var sampled = Angle.FromDegrees(
                    SampleGaussian((float) def.MinAngleMin.Value.Degrees, (float) def.MinAngleMax.Value.Degrees));
                ent.Comp.MinAngleDelta = sampled - gun.MinAngle;
            }

            if (def.MaxAngleMin.HasValue && def.MaxAngleMax.HasValue)
            {
                var sampled = Angle.FromDegrees(
                    SampleGaussian((float) def.MaxAngleMin.Value.Degrees, (float) def.MaxAngleMax.Value.Degrees));
                ent.Comp.MaxAngleDelta = sampled - gun.MaxAngle;
            }

            // Trigger RefreshModifiers to apply the deltas immediately.
            // SharedGunSystem.OnMapInit may run before or after us, so we
            // always call it ourselves to ensure the modified values are set.
            _gunSystem.RefreshModifiers((ent.Owner, gun));
        }

        // Multiplier-based spread (e.g. Hushpup). GunSpreadModifierComponent
        // has no Access restriction so it can be written directly.
        if (def.SpreadMultiplierMin.HasValue && def.SpreadMultiplierMax.HasValue)
        {
            var spreadComp = EnsureComp<GunSpreadModifierComponent>(ent.Owner);
            spreadComp.Spread = SampleGaussian(def.SpreadMultiplierMin.Value, def.SpreadMultiplierMax.Value);
            Dirty(ent.Owner, spreadComp);
        }
    }

    private void OnRefreshModifiers(Entity<GunSpreadDefectComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.MinAngle += ent.Comp.MinAngleDelta;
        args.MaxAngle += ent.Comp.MaxAngleDelta;

        // Guard: min must not exceed max after all modifiers have been applied.
        if (args.MinAngle > args.MaxAngle)
            args.MaxAngle = args.MinAngle;
    }

    // Samples from a normal distribution centred between min and max,
    // clamped so the result never escapes the provided range.
    private float SampleGaussian(float min, float max)
    {
        var mean = (min + max) * 0.5f;
        var stdDev = (max - min) * 0.25f;
        return Math.Clamp((float) _random.NextGaussian(mean, stdDev), min, max);
    }
}
