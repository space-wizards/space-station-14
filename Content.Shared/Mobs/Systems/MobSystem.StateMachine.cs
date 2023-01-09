using Content.Shared.Database;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Mobs.Systems;

public partial class MobStateSystem
{

    //Called when a new state is entered
    protected virtual void OnEnterState(MobStateComponent component, MobState state)
    {
        OnStateEnteredMiscSystems(component, state);
    }

    //Called right before stateChangedEvent gets raised
    protected virtual void OnStateChanged(MobStateComponent component, MobState oldState, MobState newState) {}

    //Called when exiting a state
    protected virtual void OnExitState(MobStateComponent component, MobState state)
    {
        OnStateExitMiscSystems(component, state);
    }

    private bool ChangeState(EntityUid origin, MobStateComponent component, MobState newState)
    {
        var oldState = component.CurrentState;
        //make sure we are allowed to enter the new state
        if (oldState == newState || !component.AllowedStates.Contains(newState))
            return false;

        OnExitState(component,oldState);
        component.CurrentState = newState;
        OnEnterState(component,oldState);

        var ev = new MobStateChangedEvent(component, oldState, newState, origin);
        OnStateChanged(component, oldState, newState);
        RaiseLocalEvent(origin, ev, true);
        _adminLogger.Add(LogType.Damaged, oldState == MobState.Alive ? LogImpact.Low : LogImpact.Medium,
            $"{ToPrettyString(component.Owner):user} state changed from {oldState} to {newState}");
        Dirty(component);
        return true;
    }


}
