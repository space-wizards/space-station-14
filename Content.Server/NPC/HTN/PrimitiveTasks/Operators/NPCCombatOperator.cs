using System.Threading;
using System.Threading.Tasks;
using Content.Server.Interaction;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
//using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public abstract class NPCCombatOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("key")] public string Key = "CombatTarget";

    /// <summary>
    /// The EntityCoordinates of the specified target.
    /// </summary>
    [DataField("keyCoordinates")]
    public string KeyCoordinates = "CombatTargetCoordinates";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var result = _entManager.System<NPCUtilitySystem>().GetEntities(blackboard, "NearbyMeleeTargets");
        var target = result.GetHighest();

        if (target == null)
        {
            return (false, new Dictionary<string, object>());
        }

        var effects = new Dictionary<string, object>()
        {
            {Key, target.Value},
            {KeyCoordinates, new EntityCoordinates(target.Value, Vector2.Zero)}
        };

        return (true, effects);
    }
}
