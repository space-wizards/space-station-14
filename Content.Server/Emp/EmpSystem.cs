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
    public const string EmpDisabledEffectPrototype = "EffectEmpDisabled";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpPulseEvent>(EmpPulse);
        SubscribeLocalEvent<EmpOnTriggerComponent, TriggerEvent>(HandleEmpTrigger);
    }

    public void EmpPulse(ref EmpPulseEvent args)
    {
        foreach (var uid in _lookup.GetEntitiesInRange(args.coordinates, args.range))
        {
            var affected = false;
            if (TryComp<BatteryComponent>(uid, out var battery))
            {
                affected = true;
                battery.UseCharge(args.energyConsumption);
            }
            if (TryComp<PoweredLightComponent>(uid, out var light))
            {
                affected = true;
                _poweredLight.TryDestroyBulb(uid, light);
            }
            if (affected)
            {
                Spawn(EmpDisabledEffectPrototype, Transform(uid).Coordinates);
            }
        }
        Spawn(EmpPulseEffectPrototype, args.coordinates);
    }

    private void HandleEmpTrigger(EntityUid uid, EmpOnTriggerComponent comp, TriggerEvent args)
    {
        var ev = new EmpPulseEvent(Transform(uid).Coordinates.ToMap(EntityManager), comp.Range, comp.EnergyConsumption);
        RaiseLocalEvent(ref ev);
        args.Handled = true;
    }
}

[ByRefEvent]
public readonly record struct EmpPulseEvent(MapCoordinates coordinates, float range, float energyConsumption);
