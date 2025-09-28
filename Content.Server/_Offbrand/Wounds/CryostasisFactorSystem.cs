using Content.Server.Temperature.Components;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.Temperature;

namespace Content.Server._Offbrand.Wounds;

public sealed class CryostasisFactorSystem : EntitySystem
{
    [Dependency] protected readonly SharedMetabolizerSystem _metabolizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryostasisFactorComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
        SubscribeLocalEvent<CryostasisFactorComponent, GetMetabolicMultiplierEvent>(OnGetMetabolicMultiplier);
    }

    private void OnTemperatureChange(Entity<CryostasisFactorComponent> ent, ref OnTemperatureChangeEvent args)
    {
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnGetMetabolicMultiplier(Entity<CryostasisFactorComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        if (!TryComp<TemperatureComponent>(ent, out var temp))
            return;

        args.Multiplier *= Math.Max(ent.Comp.TemperatureCoefficient * temp.CurrentTemperature + ent.Comp.TemperatureConstant, 1);
    }
}
