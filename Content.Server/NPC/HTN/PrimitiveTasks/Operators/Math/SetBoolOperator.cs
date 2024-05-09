using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Math;

/// <summary>
/// Just sets a blackboard key to a bool
/// </summary>
public sealed partial class SetBoolOperator : HTNOperator
{
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Value;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        return (
            true,
            new Dictionary<string, object>
            {
                { TargetKey, Value }
            }
        );
    }
}
