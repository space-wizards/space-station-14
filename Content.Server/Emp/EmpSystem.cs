using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Map;

namespace Content.Server.Emp;

public sealed class EmpSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;

    public const string EmpPulseEffectPrototype = "EffectEmpPulse";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpOnTriggerComponent, TriggerEvent>(HandleEmpTrigger);
    }

    public void EmpPulse(EntityCoordinates coordinates, float range)
    {
        Spawn(EmpPulseEffectPrototype, coordinates);
    }

    private void HandleEmpTrigger(EntityUid uid, EmpOnTriggerComponent comp, TriggerEvent args)
    {
        EmpPulse(Transform(uid).Coordinates, comp.Range);
        args.Handled = true;
    }
}
