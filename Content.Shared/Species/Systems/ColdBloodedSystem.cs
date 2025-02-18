using Content.Shared.Species.Components;
using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Content.Shared.Temperature;
using Robust.Shared.Random;

namespace Content.Shared.Species.Systems;

public sealed partial class ColdBloodedSystem : EntitySystem
{

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string StatusEffectKey = "ForcedSleep";

    [ValidatePrototypeId<AlertCategoryPrototype>]
    public const string ColdBloodedAlertCategory = "ColdBlooded";

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColdBloodedComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
    }

    private void OnTemperatureChange(EntityUid uid, ColdBloodedComponent comp, OnTemperatureChangeEvent args)
    {
        var curTemp = args.CurrentTemperature;

        if (curTemp <= comp.SleepTemperature)
            comp.HasColdTemperature = true;
        else
            comp.HasColdTemperature = false;
    }

    private void OnAlertChange(EntityUid uid, ColdBloodedComponent comp)
    {
        var type = comp.Alert;
        var alertCoof = comp.ColdCoof.Value / 100;

        switch (alertCoof)
        {
            case <= 0:
                _alerts.ClearAlertCategory(uid, ColdBloodedAlertCategory);
                break;

            case <= 25:
                _alerts.ShowAlert(uid, type, 0);
                break;

            case <= 50:
                _alerts.ShowAlert(uid, type, 1);
                break;

            case <= 75:
                _alerts.ShowAlert(uid, type, 2);
                break;

            case >= 100:
                _alerts.ShowAlert(uid, type, 3);
                break;
        }
    }

    public bool ChangeColdCoofAmount(EntityUid uid, FixedPoint2 amount, bool negative, ColdBloodedComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (negative)
            comp.ColdCoof -= amount;
        else
            comp.ColdCoof += amount;

        if (comp.ColdCoof >= comp.ColdCoofReqAmount)
        {
            _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(10), false);
            ChangeColdCoofAmount(uid, comp.ColdCoof, true);
        }

        OnAlertChange(uid, comp);

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ColdBloodedComponent>();
        while (query.MoveNext(out var uid, out var cold))
        {
            cold.Accumulator += frameTime;

            if (cold.Accumulator <= 1)
                continue;
            cold.Accumulator -= 1;

            if (cold.ColdCoof <= cold.ColdCoofReqAmount && cold.HasColdTemperature)
            {
                ChangeColdCoofAmount(uid, cold.ColdCoofPerSecond, false, cold);
            }

            if (cold.ColdCoof >= 0 && !cold.HasColdTemperature)
            {
                ChangeColdCoofAmount(uid, cold.ColdCoofPerSecond, true, cold);
            }
        }
    }
}
