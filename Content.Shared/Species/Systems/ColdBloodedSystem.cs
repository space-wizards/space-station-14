using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Species.Components;
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
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
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

    public bool ChangeSleepCoefficientAmount(EntityUid uid, FixedPoint2 amount, bool negative, ColdBloodedComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (negative)
            comp.CurrentSleepCoefficient -= amount;
        else
        {
            comp.CurrentSleepCoefficient += amount;
            PopupEntity(uid, comp);
        }

        if (comp.CurrentSleepCoefficient >= comp.SleepCoefficientReqAmount)
        {
            _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(10), false);
            ChangeSleepCoefficientAmount(uid, comp.CurrentSleepCoefficient, true);
        }

        OnAlertChange(uid, comp);

        return true;
    }

    private void PopupEntity(EntityUid uid, ColdBloodedComponent comp)
    {
        var popupChance = _random.Next(10);

        if (popupChance <= 2)
            _popupSystem.PopupEntity(Loc.GetString(comp.PopupId), uid, uid, PopupType.Medium);
    }

    private void OnAlertChange(EntityUid uid, ColdBloodedComponent comp)
    {
        var type = comp.Alert;
        var alertCoof = comp.CurrentSleepCoefficient.Value / 100;

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

            if (cold.CurrentSleepCoefficient <= cold.SleepCoefficientReqAmount && cold.HasColdTemperature)
            {
                ChangeSleepCoefficientAmount(uid, cold.SleepCoefficientPerSecond, false, cold);
            }

            if (cold.CurrentSleepCoefficient >= 1 && !cold.HasColdTemperature)
            {
                ChangeSleepCoefficientAmount(uid, cold.SleepCoefficientPerSecond, true, cold);
            }
        }
    }
}
