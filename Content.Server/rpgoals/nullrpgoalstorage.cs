using Content.Shared.RPGoals;
using Robust.Shared.Network;

namespace Content.Server.RPGoals;

public sealed class NullRPGoalStorage : IRPGoalStorage
{
    public void SaveSelection(NetUserId userId, RPGoalSession session)
    {
    }
}
