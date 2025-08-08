using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;


namespace Content.Shared._Starlight.Silicons.Borgs;

public sealed class StationAIShuntSystem : EntitySystem
{

    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAIShuntableComponent, AIShuntActionEvent>(OnAttemptShunt);
        SubscribeLocalEvent<StationAIShuntComponent, AIUnShuntActionEvent>(OnAttemptUnshunt);
    }

    private void OnAttemptShunt(EntityUid uid, StationAIShuntableComponent shuntable, AIShuntActionEvent ev)
    {
        if (ev.Handled)
            return;
        var target = ev.Target;

        if (!TryComp<StationAIShuntComponent>(target, out var shunt))
            return;
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var _))
            return;

        if (TryComp<BorgChassisComponent>(target, out var chassisComp))
        {
            var brain = chassisComp.BrainContainer.ContainedEntity;
            if (!brain.HasValue)
                return; //Chassis has no posibrian so cant shunt into it.
            if (!TryComp<StationAIShuntComponent>(brain, out var brainShunt))
                return; //Chassis brain is not able to be shunted into so obviously we cant.
            brainShunt.Return = uid;
            brainShunt.ReturnAction = _actionSystem.AddAction(brain.Value, shuntable.UnshuntAction.Id);
        }

        shunt.Return = uid;
        _mindSystem.TransferTo(mindId, target);
        shunt.ReturnAction = _actionSystem.AddAction(target, shuntable.UnshuntAction.Id);
        shuntable.Inhabited = target;
        
        ev.Handled = true;
    }

    private void OnAttemptUnshunt(EntityUid uid, StationAIShuntComponent shunt, AIUnShuntActionEvent ev)
    {
        if (ev.Handled)
            return;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var _))
            return;

        if (!TryComp<ActionComponent>(shunt.ReturnAction, out var act))
            return; //Somehow the action does not have action component? invalid perhaps?

        if (!TryComp<StationAIShuntableComponent>(shunt.Return, out var shuntable))
            return; //trying to return to a body you cant leave from? weird...
        
        if (TryComp<BorgChassisComponent>(uid, out var chassisComp))
        {
            var brain = chassisComp.BrainContainer.ContainedEntity;
            if (!brain.HasValue)
                return; //Chassis has no brain... how is the AI controlling it???
            if (!TryComp<StationAIShuntComponent>(brain, out var brainShunt))
                return; //Chassis brain is not able to be shunted into so how is AI controlling it???
            if (!TryComp<ActionComponent>(brainShunt.ReturnAction, out var brainAct))
                return; //Somehow the action does not have action component? invalid perhaps?
            _actionSystem.RemoveAction(new Entity<ActionComponent?>(brainShunt.ReturnAction.Value, brainAct));
            brainShunt.Return = null; //cause we are returning now
            brainShunt.ReturnAction = null;
        }
        
        _actionSystem.RemoveAction(new Entity<ActionComponent?>(shunt.ReturnAction.Value, act));
        _mindSystem.TransferTo(mindId, shunt.Return);
        shunt.ReturnAction = null;
        shunt.Return = null;
        shuntable.Inhabited = null;
    }
}

public sealed partial class AIShuntActionEvent : EntityTargetActionEvent
{
}

public sealed partial class AIUnShuntActionEvent : InstantActionEvent
{
}
