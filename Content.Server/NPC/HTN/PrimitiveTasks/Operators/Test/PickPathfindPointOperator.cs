using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Components;
using Robust.Shared.Random;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Test;

public sealed partial class PickPathfindPointOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        // Find all pathfind points on the same grid and choose to move to it.
        var xform = _entManager.GetComponent<TransformComponent>(owner);
        var gridUid = xform.GridUid;

        if (gridUid == null)
            return (false, null);

        var points = new List<TransformComponent>();

        foreach (var (point, pointXform) in _entManager.EntityQuery<NPCPathfindPointComponent, TransformComponent>(true))
        {
            if (gridUid != pointXform.GridUid)
                continue;

            points.Add(pointXform);
        }

        if (points.Count == 0)
            return (false, null);

        var selected = _random.Pick(points);

        return (true, new Dictionary<string, object>()
        {
            { NPCBlackboard.MovementTarget, selected.Coordinates }
        });
    }
}
