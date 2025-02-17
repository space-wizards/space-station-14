using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.Species.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Temperature;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Species;

public sealed partial class ColdBloodedSystem : EntitySystem
{

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string StatusEffectKey = "ForcedSleep";

    [ValidatePrototypeId<AlertCategoryPrototype>]
    public const string ColdBloodedAlertCategory = "ColdBlooded";

    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColdBloodedComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
    }

    private void OnTemperatureChange(EntityUid uid, ColdBloodedComponent comp, OnTemperatureChangeEvent args)
    {
        var curTemp = args.CurrentTemperature;

        if (curTemp <= comp.SleepTemperature)
            ForceToSleep(uid, comp, curTemp);
    }

    private void ForceToSleep(EntityUid uid, ColdBloodedComponent comp, float curTemp)
    {
        var type = comp.Alert;
        var temp = (comp.SleepTemperature - curTemp) * comp.TemperatureCooficient;
        var time = _timing.CurTime;

        var timeCoof = temp * time.Seconds;

        switch (timeCoof)
        {
            case <= 0f:
                _alerts.ClearAlertCategory(uid, ColdBloodedAlertCategory);
                break;

            case <= 25f:
                _alerts.ShowAlert(uid, type, 0);
                break;

            case <= 50f:
                _alerts.ShowAlert(uid, type, 1);
                break;

            case <= 75f:
                _alerts.ShowAlert(uid, type, 2);
                break;

            case > 100f:
                _alerts.ShowAlert(uid, type, 3);
                break;
        }

        if (timeCoof >= 100f)
        {
            var duration = _random.NextFloat(comp.MinDuration, comp.MaxDuration + 1);
            _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(duration), false);
            return;
        }
        return;
    }
}
