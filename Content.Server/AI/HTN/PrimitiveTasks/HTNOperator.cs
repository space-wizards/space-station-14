using System.Threading.Tasks;

namespace Content.Server.AI.HTN.PrimitiveTasks;

[ImplicitDataDefinitionForInheritors]
public abstract class HTNOperator
{
    /// <summary>
    /// Called during planning.
    /// </summary>
    public virtual async Task PlanUpdate(NPCBlackboard blackboard) {}

    /// <summary>
    /// Called during the NPC's regular updates.
    /// </summary>
    public virtual void Update(NPCBlackboard blackboard) {}
}
