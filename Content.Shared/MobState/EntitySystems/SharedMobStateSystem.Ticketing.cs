using Content.Shared.MobState.Components;

namespace Content.Shared.MobState.EntitySystems;

public abstract partial class SharedMobStateSystem
{

    protected void ClearMobStateTickets(MobStateComponent mobState, MobState state)
    {
        mobState.StateTickets[(byte) state - 1] = 0;
        //TODO: update state
    }

    protected void IncrementMobStateTickets(MobStateComponent mobState, MobState state)
    {
        mobState.StateTickets[(byte) state - 1]++;
        //TODO: update state
    }

    protected void DecrementMobStateTickets(MobStateComponent mobState, MobState state)
    {
        var temp = mobState.StateTickets[(byte) state - 1]- 1;
        if (temp < 0)
            temp = 0;
        mobState.StateTickets[(byte) state - 1] = (ushort)temp;
        //TODO: update state
    }


    public void TakeMobStateTicket(EntityUid target, MobState state, MobStateComponent? mobState)
    {
        if (!Resolve(target,ref mobState, false))
            return;
        IncrementMobStateTickets(mobState, state);
    }

    public void ReturnMobStateTicket(EntityUid target, MobState state, MobStateComponent? mobState)
    {
        if (!Resolve(target,ref mobState, false))
            return;
        DecrementMobStateTickets(mobState, state);
    }
}
