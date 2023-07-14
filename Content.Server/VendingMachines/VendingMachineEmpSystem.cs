using Content.Server.Emp;
using Content.Server.Power.EntitySystems;
using Content.Shared.Broke;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Timing;

namespace Content.Server.VendingMachines;

public sealed class VendingMachineEmpSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrokeComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(EntityUid uid, BrokeComponent component,
        ref EmpPulseEvent args)
    {
        if (!TryComp<VendingMachineEmpEjectComponent>(uid, out var empEjectComponent))
            return;

        if (!component.Broken && this.IsPowered(uid, EntityManager))
        {
            args.Affected = true;
            args.Disabled = true;
            empEjectComponent.NextEmpEject = _timing.CurTime;
        }
    }
}
