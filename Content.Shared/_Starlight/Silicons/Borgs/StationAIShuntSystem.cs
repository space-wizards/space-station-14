using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;


namespace Content.Shared._Starlight.Silicons.Borgs;

public sealed class StationAIShuntSystem : EntitySystem
{
    
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAIShuntableComponent, AIShuntActionEvent>(OnAttemptShunt);
    }

    [SuppressMessage("Usage", "RA0030:Consider using the non-generic variant of this method")]
    private void OnAttemptShunt(EntityUid uid, StationAIShuntableComponent _, AIShuntActionEvent ev)
    {
        if (ev.Handled)
            return;
        
        var target = ev.Target;
        EntityUid? chassis = null;
        if (TryComp<BorgChassisComponent>(target, out var chassisComp))
        {
            var brainContainer = chassisComp.BrainContainer;
            var contained = brainContainer.ContainedEntity;
            if (!contained.HasValue)
                return; // a chassis without a brain? obviously we cant shunt into it.
            chassis = target; // so we can transfer the mind into this chassis
            target = contained.Value; // At this point we know it is not null so we safely set target
        }
        if (!TryComp<StationAIShuntComponent>(target, out var shunt))
            return;
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mindComp))
            return;
        _mindSystem.Visit(mindId, target, mindComp);
        if (chassis != null)
        {
            _mindSystem.TransferTo(mindId, chassis, mind: mindComp); // what if we just... yoinked the logic that is done internally when a borg brain is inserted.
        }
        ev.Handled = true;
    }
}

public sealed partial class AIShuntActionEvent : EntityTargetActionEvent
{
}

public sealed partial class AIUnShuntActionEvent : InstantActionEvent
{
}
