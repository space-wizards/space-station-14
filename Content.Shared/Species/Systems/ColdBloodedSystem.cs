using Content.Shared.Bed.Sleep;
using Content.Shared.Species.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Temperature;
using Robust.Shared.Random;

namespace Content.Shared.Species;

public sealed partial class ColdBloodedSystem : EntitySystem
{

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string StatusEffectKey = "ForcedSleep";

    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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
        var temp = _random.NextFloat(comp.SleepTemperature - curTemp, curTemp * 2);
        if (temp <= comp.SleepTemperature)
            _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(uid, StatusEffectKey, TimeSpan.FromSeconds(comp.Duration), false);
        return;
    }
}
