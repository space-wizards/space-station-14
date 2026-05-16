using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.Temperature.Components;

namespace Content.Shared._Offbrand.Organs;

public sealed partial class DamageOnInhaledAirOrganSystem : EntitySystem
{
    [Dependency] private DamageableOrganSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnInhaledAirOrganComponent, BodyRelayedEvent<BeforeInhaledGasEvent>>(OnBeforeInhaledGas);
    }

    private void OnBeforeInhaledGas(Entity<DamageOnInhaledAirOrganComponent> ent, ref BodyRelayedEvent<BeforeInhaledGasEvent> args)
    {
        if (!TryComp<TemperatureDamageComponent>(args.Body, out var temperature))
            return;

        var heatDamageThreshold = temperature.ParentHeatDamageThreshold ?? temperature.HeatDamageThreshold;
        var coldDamageThreshold = temperature.ParentColdDamageThreshold ?? temperature.ColdDamageThreshold;

        if (args.Args.Gas.Temperature >= heatDamageThreshold)
        {
            var damage = ent.Comp.HeatCoefficient * args.Args.Gas.Temperature + ent.Comp.HeatConstant;
            _damageable.ChangeDamage(ent.Owner, damage);
        }
        else if (args.Args.Gas.Temperature <= coldDamageThreshold)
        {
            var damage = ent.Comp.ColdCoefficient * args.Args.Gas.Temperature + ent.Comp.ColdConstant;
            _damageable.ChangeDamage(ent.Owner, damage);
        }
    }
}
