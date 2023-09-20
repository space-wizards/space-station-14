using Content.Server.Emp;
using Content.Server.Power.EntitySystems;
using Content.Shared.Broke;
using Content.Shared.VendingMachines.Components;

namespace Content.Server.VendingMachines;

public sealed partial class VendingMachineSystem
{
    private void OnEmpPulse(EntityUid uid, BrokeComponent component,
        ref EmpPulseEvent args)
    {
        if (!TryComp<VendingMachineEmpEjectComponent>(uid, out var empEjectComponent))
            return;

        if (!component.IsBroken && this.IsPowered(uid, EntityManager))
        {
            args.Affected = true;
            args.Disabled = true;
            empEjectComponent.NextEmpEject = _timing.CurTime;
        }
    }
}
