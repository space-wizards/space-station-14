using Content.Shared.MobState.Components;

namespace Content.Shared.MobState.EntitySystems;

public abstract partial class SharedMobStateSystem
{

    public void CheckTickets(EntityUid target, MobStateComponent? component)
    {
        if (!Resolve(target, ref component))
            return;
        CheckTickets_Internal(target, component);
    }

    //Checks the mobstate tickets and updates the current mobstate if appropriate
    private void CheckTickets_Internal(EntityUid origin, MobStateComponent component)
    {
        for (var i = component.StateTickets.Length-1; i >= 0; i--)
        {
            if (component.StateTickets[i] <= 0 || !ChangeState(origin, component, (MobState) (i + 1)))
                continue;
            Dirty(component);
            return;
        }
    }

    public void ClearMobStateTickets(EntityUid target, MobStateComponent mobState, MobState state)
    {
        mobState.StateTickets[(byte) state - 1] = 0;
        CheckTickets_Internal(target, mobState);
        Dirty(mobState);
    }

    protected void IncrementMobStateTickets(EntityUid target, MobStateComponent mobState, MobState state)
    {
        mobState.StateTickets[(byte) state - 1]++;
        CheckTickets_Internal(target, mobState);
        Dirty(mobState);
    }

    protected void DecrementMobStateTickets(EntityUid target, MobStateComponent mobState, MobState state)
    {
        var temp = mobState.StateTickets[(byte) state - 1]- 1;
        if (temp < 0)
            temp = 0;
        mobState.StateTickets[(byte) state - 1] = (ushort)temp;
        CheckTickets_Internal(target, mobState);
        Dirty(mobState);
    }


    public void TakeMobStateTicket(EntityUid target, MobState state, MobStateComponent? mobState)
    {
        if (!Resolve(target,ref mobState, false))
            return;
        IncrementMobStateTickets(target, mobState, state);
    }

    public void ReturnMobStateTicket(EntityUid target, MobState state, MobStateComponent? mobState)
    {
        if (!Resolve(target,ref mobState, false))
            return;
        DecrementMobStateTickets(target, mobState, state);
    }
}
