using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Database;
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
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

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
            // No point calculating things if we aren't in water
            GasMixture? mixture = _atmo.GetContainingMixture(uid);
            if (!(mixture != null && mixture.GetMoles(9) >= inWater.WaterThreshold))
            {
                if (inWater.TakingDamage)
                {
                    inWater.TakingDamage = false;
                    //Look at me i'm even being proper with logging
                    _adminLog.Add(LogType.Electrocution, $"Entity {uid} is no longer taking damage from water.");
                }
                continue;
            }

            var totalDamage = FixedPoint2.Zero;
            foreach (var (damageType, _) in inWater.Damage.DamageDict)
            {
                if (!damageable.Damage.DamageDict.TryGetValue(damageType, out var damage))
                    continue;
                totalDamage += damage;
            }

            if (totalDamage >= inWater.MaxDamage)
            {
                continue;
            }

            _damageable.TryChangeDamage(uid, inWater.Damage, true);
            if (!inWater.TakingDamage)
            {
                inWater.TakingDamage = true;
                _adminLog.Add(LogType.Electrocution, $"Entity {uid} is now taking damage from water.");
            }

        }
    }
}
