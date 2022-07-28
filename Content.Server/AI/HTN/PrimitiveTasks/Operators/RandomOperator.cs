using Robust.Shared.Random;

namespace Content.Server.AI.HTN.PrimitiveTasks;

public sealed class RandomOperator : HTNOperator
{
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Target blackboard key to set the value to
    /// </summary>
    [DataField("key")] public string TargetKey = string.Empty;

    [DataField("minKey", required: true)] public string MinKey = string.Empty;

    [DataField("maxKey", required: true)] public string MaxKey = string.Empty;
}
