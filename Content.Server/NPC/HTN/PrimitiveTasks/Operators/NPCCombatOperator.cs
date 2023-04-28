using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Systems;
using Robust.Shared.Map;

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

        if (!target.IsValid())
        {
            return (false, new Dictionary<string, object>());
        }

        var effects = new Dictionary<string, object>()
        {
            {Key, target},
            {KeyCoordinates, new EntityCoordinates(target, Vector2.Zero)}
        };

        return (true, effects);
    }
}
