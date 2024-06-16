using Content.Shared.Database;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Mobs.Systems;

public partial class MobStateSystem
{
    #region Public API

    /// <summary>
    /// Check if an Entity can be set to a particular MobState
    /// </summary>
    /// <param name="entity">Target Entity</param>
    /// <param name="mobState">MobState to check</param>
    /// <param name="component">MobState Component owned by the target</param>
    /// <returns>If the entity can be set to that MobState</returns>
    public bool HasState(EntityUid entity, MobState mobState, MobStateComponent? component = null)
    {
        return _mobStateQuery.Resolve(entity, ref component, false) &&
               component.AllowedStates.Contains(mobState);
    }

    /// <summary>
    /// Run a MobState update check. This will trigger update events if the state has been changed.
    /// </summary>
    /// <param name="entity">Target Entity we want to change the MobState of</param>
    /// <param name="component">MobState Component attached to the entity</param>
    /// <param name="origin">Entity that caused the state update (if applicable)</param>
    public void UpdateMobState(EntityUid entity, MobStateComponent? component = null, EntityUid? origin = null)
    {
        if (!_mobStateQuery.Resolve(entity, ref component))
            return;

        var ev = new UpdateMobStateEvent {Target = entity, Component = component, Origin = origin};
        RaiseLocalEvent(entity, ref ev);
        ChangeState(entity, component, ev.State, origin: origin);
    }

    /// <summary>
    /// Change the MobState without triggering UpdateMobState events.
    /// WARNING: use this sparingly when you need to override other systems (MobThresholds)
    /// </summary>
    /// <param name="entity">Target Entity we want to change the MobState of</param>
    /// <param name="mobState">The new MobState we want to set</param>
    /// <param name="component">MobState Component attached to the entity</param>
    /// <param name="origin">Entity that caused the state update (if applicable)</param>
    public void ChangeMobState(EntityUid entity, MobState mobState, MobStateComponent? component = null,
        EntityUid? origin = null)
    {
        if (!_mobStateQuery.Resolve(entity, ref component))
            return;

        ChangeState(entity, component, mobState, origin: origin);
    }

    #endregion

    #region Virtual API

    /// <summary>
    /// Called when a new MobState is entered.
    /// </summary>
    /// <param name="entity">The owner of the MobState Component</param>
    /// <param name="component">MobState Component owned by the target</param>
    /// <param name="state">The new MobState</param>
    protected virtual void OnEnterState(EntityUid entity, MobStateComponent component, MobState state)
    {
        OnStateEnteredSubscribers(entity, component, state);
    }

    /// <summary>
    ///  Called when this entity changes MobState
    /// </summary>
    /// <param name="entity">The owner of the MobState Component</param>
    /// <param name="component">MobState Component owned by the target</param>
    /// <param name="oldState">The previous MobState</param>
    /// <param name="newState">The new MobState</param>
    protected virtual void OnStateChanged(EntityUid entity, MobStateComponent component, MobState oldState,
        MobState newState)
    {
    }

    /// <summary>
    /// Called when a new MobState is exited.
    /// </summary>
    /// <param name="entity">The owner of the MobState Component</param>
    /// <param name="component">MobState Component owned by the target</param>
    /// <param name="state">The old MobState</param>
    protected virtual void OnExitState(EntityUid entity, MobStateComponent component, MobState state)
    {
        OnStateExitSubscribers(entity, component, state);
    }

    #endregion

    #region Private Implementation

    //Actually change the MobState
    private void ChangeState(EntityUid target, MobStateComponent component, MobState newState, EntityUid? origin = null)
    {
        var oldState = component.CurrentState;
        //make sure we are allowed to enter the new state
        if (oldState == newState || !component.AllowedStates.Contains(newState))
            return;

        OnExitState(target, component, oldState);
        component.CurrentState = newState;
        OnEnterState(target, component, newState);

        var ev = new MobStateChangedEvent(target, component, oldState, newState, origin);
        OnStateChanged(target, component, oldState, newState);
        RaiseLocalEvent(target, ev, true);
        _adminLogger.Add(LogType.Damaged, oldState == MobState.Alive ? LogImpact.Low : LogImpact.Medium,
            $"{ToPrettyString(target):user} state changed from {oldState} to {newState}");
        Dirty(target, component);
    }

    #endregion
}

/// <summary>
/// Event that gets triggered when we want to update the mobstate. This allows for systems to override MobState changes
/// </summary>
/// <param name="Target">The Entity whose MobState is changing</param>
/// <param name="Component">The MobState Component owned by the Target</param>
/// <param name="State">The new MobState we want to set</param>
/// <param name="Origin">Entity that caused the state update (if applicable)</param>
[ByRefEvent]
public record struct UpdateMobStateEvent(EntityUid Target, MobStateComponent Component, MobState State,
    EntityUid? Origin = null);
