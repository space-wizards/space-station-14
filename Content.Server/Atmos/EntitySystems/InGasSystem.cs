using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;


namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class InGasSystem : EntitySystem
{
    private const float UpdateTimer = 1f;
    private float _timer = 0f;
    [Dependency] private readonly AtmosphereSystem _atmo = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        //Subscribe to relevant events here.
        //SubscribeLocalEvent<>();
    }

    public bool InGas(EntityUid uid, int? gasId = null, float? gasThreshold = null)
    {
        var mixture = _atmo.GetContainingMixture(uid);
        var inGas = EntityManager.GetComponent<InGasComponent>(uid);
        //Use provided data if no component present
        if (inGas == null)
        {
            if (gasId == null || gasThreshold == null)
            {
                throw new Exception("Missing gasId and/or gasThreshold in InGas call");
            }

            return (mixture != null && mixture.GetMoles((int)gasId) >= gasThreshold);
        }

        //If we are not in the gas return false, else true
        return (mixture != null && mixture.GetMoles(inGas.GasId) >= inGas.GasThreshold);
    }

    public override void Update(float frameTime)
    {
        _timer += frameTime;

        if (_timer < UpdateTimer)
            return;

        _timer -= UpdateTimer;

        var enumerator = EntityQueryEnumerator<InGasComponent, DamageableComponent>();
        while (enumerator.MoveNext(out var uid, out var inGas, out var damageable))
        {
            if (!inGas.DamagedByGas)
            {
                continue;
            }
            // No point calculating things if we aren't in water

            if (!InGas(uid))
            {
                if (inGas.TakingDamage)
                {
                    inGas.TakingDamage = false;
                    //Look at me i'm even being proper with logging
                    _adminLog.Add(LogType.Electrocution, $"Entity {uid} is no longer taking damage from water.");
                }
                continue;
            }

            var totalDamage = FixedPoint2.Zero;
            foreach (var (damageType, _) in inGas.Damage.DamageDict)
            {
                if (!damageable.Damage.DamageDict.TryGetValue(damageType, out var damage))
                    continue;
                totalDamage += damage;
            }

            if (totalDamage >= inGas.MaxDamage)
            {
                continue;
            }

            _damageable.TryChangeDamage(uid, inGas.Damage, true);
            if (!inGas.TakingDamage)
            {
                inGas.TakingDamage = true;
                _adminLog.Add(LogType.Electrocution, $"Entity {uid} is now taking damage from water.");
            }

            switch (inGas.TakingDamage)
            {
                case true:
                    _alerts.ShowAlert(uid, inGas.DrowningAlert, 1);
                    break;
                default:
                    _alerts.ClearAlertCategory(uid, inGas.BreathingAlertCategory);
                    break;
            }

        }
    }
}
