namespace Content.Server.NPC.HTN;

/// <summary>
/// Helper interface to run the appropriate shutdown for a particular task.
/// </summary>
public interface IHtnConditionalShutdown
{
    /// <summary>
    /// When to shut the task down.
    /// </summary>
    HTNPlanState ShutdownOnState { get; }

    /// <summary>
    /// Run whenever the <see cref="ShutdownOnState"/> specifies.
    /// </summary>
    void ConditionalShutdown(NPCBlackboard blackboard);
}
