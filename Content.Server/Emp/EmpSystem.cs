using Content.Server.Explosion.EntitySystems;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Power.Components;
using Robust.Shared.Map;

namespace Content.Server.Emp;

public sealed class EmpSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;

    public const string EmpPulseEffectPrototype = "EffectEmpPulse";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpOnTriggerComponent, TriggerEvent>(HandleEmpTrigger);
    }

    public void EmpPulse(EntityCoordinates coordinates, float range, float energyConsumption)
    {
        foreach (var uid in _lookup.GetEntitiesInRange(coordinates.ToMap(EntityManager), range))
        {
            if (TryComp<BatteryComponent>(uid, out var battery))
                battery.UseCharge(energyConsumption);
            if (TryComp<PoweredLightComponent>(uid, out var light))
                _poweredLight.TryDestroyBulb(light.Owner, light);
        }
        Spawn(EmpPulseEffectPrototype, coordinates);
    }

    private void HandleEmpTrigger(EntityUid uid, EmpOnTriggerComponent comp, TriggerEvent args)
    {
        EmpPulse(Transform(uid).Coordinates, comp.Range, comp.EnergyConsumption);
        args.Handled = true;
    }
}
