using Content.Shared.Emp;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class EmpOnTriggerSystem : XOnTriggerSystem<EmpOnTriggerComponent>
{
    [Dependency] private readonly SharedEmpSystem _emp = default!;

    protected override void OnTrigger(Entity<EmpOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _emp.EmpPulse(Transform(target).Coordinates, ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration, args.User);
        args.Handled = true;
    }
}
