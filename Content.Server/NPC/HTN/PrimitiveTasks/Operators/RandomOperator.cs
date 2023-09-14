using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Random;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class RandomOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Target blackboard key to set the value to. Doesn't need to exist beforehand.
    /// </summary>
    [DataField("targetKey", required: true)] public string TargetKey = string.Empty;

    /// <summary>
    ///  Minimum idle time.
    /// </summary>
    [DataField("minKey", required: true)] public string MinKey = string.Empty;

    /// <summary>
    ///  Maximum idle time.
    /// </summary>
    [DataField("maxKey", required: true)] public string MaxKey = string.Empty;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        return (true, new Dictionary<string, object>()
        {
            {
                TargetKey,
                _random.NextFloat(blackboard.GetValueOrDefault<float>(MinKey, _entManager),
                    blackboard.GetValueOrDefault<float>(MaxKey, _entManager))
            }
        });
    }
}
