using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Math;

/// <summary>
/// Added <see cref="AddFloatOperator.Amount"/> to float value for the
/// specified <see cref="AddFloatOperator.TargetKey"/> in the <see cref="NPCBlackboard"/>.
/// </summary>
public sealed partial class AddFloatOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true), ViewVariables]
    public string TargetKey = string.Empty;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Amount;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<float>(TargetKey, out var value, _entManager))
            return (false, null);

        return (
            true,
            new Dictionary<string, object>
            {
                { TargetKey, value + Amount }
            }
        );
    }
}
