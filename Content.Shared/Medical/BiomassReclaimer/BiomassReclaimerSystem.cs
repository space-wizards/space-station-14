using Content.Shared.Botany.Items.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Medical.BiomassReclaimer;

public abstract partial class BiomassReclaimerSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;

    [Dependency] protected EntityQuery<TransformComponent> TransformQuery;
    [Dependency] protected EntityQuery<ProduceComponent> ProduceQuery;

    protected bool IsPowered(EntityUid uid) => _powerReceiver.IsPowered(uid);

    protected virtual bool CanProcess(Entity<BiomassReclaimerComponent> reclaimer, EntityUid target)
    {
        var isPlant = ProduceQuery.HasComp(target);
        if (!isPlant && !HasComp<MobStateComponent>(target))
            return false;

        if (!TransformQuery.GetComponent(reclaimer).Anchored || !IsPowered(reclaimer))
            return false;

        return isPlant || !reclaimer.Comp.SafetyEnabled || _mobState.IsDead(target);
    }
}
