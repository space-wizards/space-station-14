namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Waits the specified amount of time. Removes the key when finished.
/// </summary>
public sealed class WaitOperator : HTNOperator
{
    /// <summary>
    /// Blackboard key for the time we'll wait for.
    /// </summary>
    [DataField("key", required: true)] public string Key = string.Empty;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<float>(Key, out var timer))
        {
            return HTNOperatorStatus.Finished;
        }

        timer -= frameTime;
        blackboard.SetValue(Key, timer);

        return timer <= 0f ? HTNOperatorStatus.Finished : HTNOperatorStatus.Continuing;
    }

    public override void Shutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.Shutdown(blackboard, status);

        // The replacement plan may want this value so only dump it if we're successful.
        if (status != HTNOperatorStatus.BetterPlan)
        {
            blackboard.Remove<float>(Key);
        }
    }
}
