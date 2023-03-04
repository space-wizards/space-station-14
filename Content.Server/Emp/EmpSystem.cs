using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Map;

namespace Content.Server.Emp;

public sealed class EmpSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public const string EmpPulseEffectPrototype = "EffectEmpPulse";
    public const string EmpDisabledEffectPrototype = "EffectEmpDisabled";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpOnTriggerComponent, TriggerEvent>(HandleEmpTrigger);
    }

    public void EmpPulse(MapCoordinates coordinates, float range, float energyConsumption)
    {
        foreach (var uid in _lookup.GetEntitiesInRange(coordinates, range))
        {
            var ev = new EmpPulseEvent(energyConsumption, false);
            RaiseLocalEvent(uid, ref ev);
            if (ev.Affected)
                Spawn(EmpDisabledEffectPrototype, Transform(uid).Coordinates);
        }
        Spawn(EmpPulseEffectPrototype, coordinates);
    }

    private void HandleEmpTrigger(EntityUid uid, EmpOnTriggerComponent comp, TriggerEvent args)
    {
        EmpPulse(Transform(uid).Coordinates.ToMap(EntityManager), comp.Range, comp.EnergyConsumption);
        args.Handled = true;
    }
}

[ByRefEvent]
public record struct EmpPulseEvent(float EnergyConsumption, bool Affected);
