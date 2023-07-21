namespace Content.Server.NPC.HTN;

[Flags]
public enum HTNPlanState : byte
{
    Running = 1 << 0,

    PlanFinished = 1 << 1,
}
