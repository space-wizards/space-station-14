using Robust.Shared.Serialization;

namespace Content.Shared.NPC.NuPC;

public abstract class SharedNpcGoalSystem : EntitySystem
{

}

[Serializable, NetSerializable]
public sealed class RequestNpcGoalsEvent : EntityEventArgs
{
    public bool Enabled;
}

[Serializable, NetSerializable]
public sealed class NpcGoalsDebugEvent : EntityEventArgs
{
    public List<NpcGoalsData> Data = new();
}

[Serializable, NetSerializable]
public record struct NpcGoalsData
{
    public List<INpcGoalGenerator> Generators;

    public List<INpcGoal> Goals;
}

/// <summary>
/// Generates a combat goal to attack hostile mobs.
/// </summary>
public partial record struct NpcCombatGoalGenerator : INpcGoalGenerator
{

}

/// <summary>
/// Generates a goal to investigate a last known hostile mob position.
/// </summary>
public partial record struct NpcChaseGoalGenerator : INpcGoalGenerator;

/// <summary>
/// Generates goals
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface INpcGoalGenerator {}

/// <summary>
/// Represents "compound stims" for an NPC.
/// E.g. If an NPC has 5 footstep stims from an unknown source they might generate a goal to investigate it.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface INpcGoal
{
    /// <summary>
    /// Has the goal been updated for this tick. If not then it is removed.
    /// </summary>
    public bool Updated { get; set; }
}

[Serializable, NetSerializable]
public partial record struct NpcCombatGoal : INpcGoal
{
    /// <inheritdoc />
    public bool Updated { get; set; }
}

[Serializable, NetSerializable]
public partial record struct NpcChaseGoal : INpcGoal
{
    /// <inheritdoc />
    public bool Updated { get; set; }
}
