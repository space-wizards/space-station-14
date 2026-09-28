using Content.Shared.Botany.Items.Components;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Medical.BiomassReclaimer;

public abstract partial class BiomassReclaimerSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] protected SharedPowerReceiverSystem _powerReceiver = default!;

    [Dependency] protected EntityQuery<TransformComponent> _transformQuery;
    [Dependency] protected EntityQuery<ProduceComponent> _produceQuery;
    [Dependency] protected EntityQuery<PhysicsComponent> _physicsQuery;

    [SubscribeLocalEvent]
    private void OnCanDrop(Entity<BiomassReclaimerComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = CanInsert(ent, args.Dragged) && _physicsQuery.HasComp(args.Dragged);
        args.Handled = true;
    }

    protected virtual bool CanInsert(Entity<BiomassReclaimerComponent> reclaimer, EntityUid target)
    {
        var isPlant = _produceQuery.HasComp(target);
        if (!isPlant && !HasComp<MobStateComponent>(target))
            return false;

        if (!_transformQuery.GetComponent(reclaimer).Anchored || !_powerReceiver.IsPowered(reclaimer.Owner))
            return false;

        return isPlant || !reclaimer.Comp.SafetyEnabled || _mobState.IsDead(target);
    }
}
