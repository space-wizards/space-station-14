using System.Threading.Tasks;

namespace Content.Server.AI.HTN.PrimitiveTasks;

[ImplicitDataDefinitionForInheritors]
public abstract class HTNOperator
{
    /// <summary>
    /// Called once whenever prototypes reload. Typically used to inject dependencies.
    /// </summary>
    public virtual void Initialize()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Called during planning.
    /// </summary>
    /// <param name="blackboard">The blackboard for the NPC.</param>
    /// <returns>The effects to apply to the blackboard.</returns>
    public virtual async Task<Dictionary<string, object>?> Plan(NPCBlackboard blackboard)
    {
        return null;
    }

    /// <summary>
    /// Called during the NPC's regular updates.
    /// </summary>
    public virtual HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        return HTNOperatorStatus.Finished;
    }

    /// <summary>
    /// Called the first time an operator runs.
    /// </summary>
    public virtual void Startup(NPCBlackboard blackboard) {}

    /// <summary>
    /// Called whenever the operator stops running.
    /// </summary>
    public virtual void Shutdown(NPCBlackboard blackboard, HTNOperatorStatus status) {}
}
