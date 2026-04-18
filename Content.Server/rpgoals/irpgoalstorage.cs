using Content.Shared.RPGoals;
using Robust.Shared.Network;

namespace Content.Server.RPGoals;

public interface IRPGoalStorage
{
    void SaveSelection(NetUserId userId, RPGoalSession session);
}
