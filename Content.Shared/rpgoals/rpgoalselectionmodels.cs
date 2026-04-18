using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.RPGoals;

[Serializable, NetSerializable]
public sealed record RPGoalOption(string GoalId, string LocaleKey, string Category);

[Serializable, NetSerializable]
public sealed class RPGoalSelectionState : EntityEventArgs
{
    public IReadOnlyList<RPGoalOption> Options { get; }
    public int RerollsRemaining { get; }
    public bool Finalized { get; }

    public RPGoalSelectionState(IReadOnlyList<RPGoalOption> options, int rerollsRemaining, bool finalized)
    {
        Options = options;
        RerollsRemaining = rerollsRemaining;
        Finalized = finalized;
    }
}

[Serializable, NetSerializable]
public sealed class RPGoalSelectionRequest : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class RPGoalAcceptMessage : EntityEventArgs
{
    public string GoalId { get; }

    public RPGoalAcceptMessage(string goalId)
    {
        GoalId = goalId;
    }
}

[Serializable, NetSerializable]
public sealed class RPGoalRerollMessage : EntityEventArgs;
