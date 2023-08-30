using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Random;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class PickRandomRotationOperator : HTNOperator
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [DataField("targetKey")]
    public string TargetKey = "RotateTarget";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var rotation = _random.NextAngle();
        return (true, new Dictionary<string, object>()
        {
            {TargetKey, rotation}
        });
    }
}
