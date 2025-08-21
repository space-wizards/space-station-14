using Content.Shared.Emp;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class EmpOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedEmpSystem _emp = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<EmpOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        _emp.EmpPulse(Transform(target.Value).Coordinates, ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration, args.User);
        args.Handled = true;
    }
}
