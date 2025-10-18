using Content.Shared.Emp;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedChargerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChargerComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(EntityUid uid, ChargerComponent component, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;
    }
}
