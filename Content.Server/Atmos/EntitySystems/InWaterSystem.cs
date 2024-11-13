using Content.Server.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;


namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class InWaterSystem : EntitySystem
{
    private const float UpdateTimer = 1f;
    private float _timer = 0f;
    [Dependency] private readonly AtmosphereSystem _atmo = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        //Subscribe to relevant events here.
        //SubscribeLocalEvent<>();
    }

    public override void Update(float frameTime)
    {
        _timer += frameTime;

        if (_timer < UpdateTimer)
            return;

        _timer -= UpdateTimer;

        var enumerator = EntityQueryEnumerator<InWaterComponent, DamageableComponent>();
        while (enumerator.MoveNext(out var uid, out var inWater, out var damageable))
        {
            if (!inWater.DamagedByWater)
            {
                continue;
            }
            var totalDamage = FixedPoint2.Zero;
            foreach (var (barotraumaDamageType, _) in inWater.Damage.DamageDict)
            {
                if (!damageable.Damage.DamageDict.TryGetValue(barotraumaDamageType, out var damage))
                    continue;
                totalDamage += damage;
            }

            //if (totalDamage >= gasComp.MaxDamage)
            //    continue;

            if (_atmo.GetContainingMixture(uid) is { } mixture)
            {

            }
        }
    }
}
