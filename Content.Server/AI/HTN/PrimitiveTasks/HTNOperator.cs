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
    public virtual async Task PlanUpdate(NPCBlackboard blackboard) {}

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
