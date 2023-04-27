using Content.Server.NPC.Queries;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Handles utility queries for NPCs.
/// </summary>
public sealed class NPCUtilitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly FactionSystem _faction = default!;

    public UtilityResult GetEntities(NPCBlackboard blackboard, string proto)
    {
        var weh = _proto.Index<UtilityQueryPrototype>(proto);
        var ents = new HashSet<EntityUid>();

        foreach (var query in weh.Query)
        {
            switch (query)
            {
                case UtilityQueryFilter filter:
                    Filter(blackboard, ents, filter);
                    break;
                default:
                    Add(blackboard, ents, query);
                    break;
            }
        }

        if (ents.Count == 0)
            return UtilityResult.Empty;

        foreach (var con in weh.Considerations)
        {

        }

        var result = new UtilityResult();
        return result;
    }

    private void Add(NPCBlackboard blackboard, HashSet<EntityUid> entities, UtilityQuery query)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        switch (query)
        {
            case NearbyHostilesQuery:

                break;
        }
    }

    private void Filter(NPCBlackboard blackboard, HashSet<EntityUid> entities, UtilityQueryFilter filter)
    {

    }
}

public readonly record struct UtilityResult(List<(EntityUid Entity, float Score)> Entities)
{
    public static readonly UtilityResult Empty = new();

    public readonly List<(EntityUid Entity, float Score)> Entities = Entities;
}
