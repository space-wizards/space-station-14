namespace Content.Server.NPC.HTN;

[ImplicitDataDefinitionForInheritors]
public abstract partial class HTNTask
{
    /// <summary>
    /// Limit the amount of tasks the planner considers. Exceeding this value sleeps the NPC and throws an exception.
    /// The expected way to hit this limit is with badly written recursive tasks.
    /// </summary>
    [DataField]
    public int MaximumTasks = 1000;
}
