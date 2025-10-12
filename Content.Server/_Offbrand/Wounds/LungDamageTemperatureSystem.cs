using Content.Server.Temperature.Components;
using Content.Shared._Offbrand.Wounds;

namespace Content.Server._Offbrand.Wounds;

public sealed class LungDamageTemperatureSystem : EntitySystem
{
    [Dependency] private readonly LungDamageSystem _lungDamage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LungDamageOnInhaledAirTemperatureComponent, BeforeInhaledGasEvent>(OnBeforeInhaledGas);
    }

    private void OnBeforeInhaledGas(Entity<LungDamageOnInhaledAirTemperatureComponent> ent, ref BeforeInhaledGasEvent args)
    {
        var temperature = Comp<TemperatureComponent>(ent);

        var heatDamageThreshold = temperature.ParentHeatDamageThreshold ?? temperature.HeatDamageThreshold;
        var coldDamageThreshold = temperature.ParentColdDamageThreshold ?? temperature.ColdDamageThreshold;

        if (args.Gas.Temperature >= heatDamageThreshold)
        {
            var damage = ent.Comp.HeatCoefficient * args.Gas.Temperature + ent.Comp.HeatConstant;
            _lungDamage.TryModifyDamage(ent.Owner, damage);
        }
        else if (args.Gas.Temperature <= coldDamageThreshold)
        {
            var damage = ent.Comp.ColdCoefficient * args.Gas.Temperature + ent.Comp.ColdConstant;
            _lungDamage.TryModifyDamage(ent.Owner, damage);
        }
    }
}
