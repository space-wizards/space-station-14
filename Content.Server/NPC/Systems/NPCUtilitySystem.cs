using System.Linq;
using Content.Server.NPC.Queries;
using Content.Server.NPC.Queries.Curves;
using Content.Server.NPC.Queries.Queries;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Handles utility queries for NPCs.
/// </summary>
public sealed class NPCUtilitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly FactionSystem _faction = default!;

    /// <summary>
    /// Runs the UtilityQueryPrototype and returns the best-matching entities.
    /// </summary>
    /// <param name="bestOnly">Should we only return the entity with the best score.</param>
    public UtilityResult GetEntities(
        NPCBlackboard blackboard,
        string proto,
        bool bestOnly = true)
    {
        // TODO: PickHostilesop or whatever needs to juse be UtilityQueryOperator

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

        var results = new Dictionary<EntityUid, float>();
        var count = 0;
        var highestScore = 0f;

        foreach (var ent in ents)
        {
            count++;

            if (count > weh.Limit)
                break;

            var score = 1f;

            foreach (var con in weh.Considerations)
            {
                var curve = con.Curve;
                float curveScore;

                switch (curve)
                {
                    case BoolCurve boolCurve:
                        curveScore = score > 0f ? 1f : 0f;
                        break;
                    case InverseBoolCurve inverseBoolCurve:
                        curveScore = score.Equals(0f) ? 1f : 0f;
                        break;
                    case PresetCurve presetCurve:
                        throw new NotImplementedException();
                        break;
                    case QuadraticCurve quadraticCurve:
                        curveScore = Math.Clamp(quadraticCurve.Slope * (float) Math.Pow(score - quadraticCurve.XOffset, quadraticCurve.Exponent) + quadraticCurve.YOffset, 0f, 1f);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var adjusted = GetAdjustedScore(curveScore, weh.Considerations.Count);
                score *= adjusted;

                // If the score can no longer go up OR we only care about best entity then early out.
                if (score <= 0f || bestOnly && score <= highestScore)
                {
                    break;
                }
            }

            if (score <= 0f)
                continue;

            highestScore = MathF.Max(score, highestScore);
            results.Add(ent, score);
        }

        var result = new UtilityResult(results);
        return result;
    }

    private float GetAdjustedScore(float score, int considerations)
    {
        /*
        * Now using the geometric mean
        * for n scores you take the n-th root of the scores multiplied
        * e.g. a, b, c scores you take Math.Pow(a * b * c, 1/3)
        * To get the ACTUAL geometric mean at any one stage you'd need to divide by the running consideration count
        * however, the downside to this is it will fluctuate up and down over time.
        * For our purposes if we go below the minimum threshold we want to cut it off, thus we take a
        * "running geometric mean" which can only ever go down (and by the final value will equal the actual geometric mean).
        */

        var adjusted = MathF.Pow(score, 1 / (float) considerations);
        return Math.Clamp(adjusted, 0f, 1f);
    }

    private void Add(NPCBlackboard blackboard, HashSet<EntityUid> entities, UtilityQuery query)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var vision = blackboard.GetValue<float>(NPCBlackboard.VisionRadius);

        switch (query)
        {
            case NearbyHostilesQuery:
                foreach (var ent in _faction.GetNearbyHostiles(owner, vision))
                {
                    entities.Add(ent);
                }
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void Filter(NPCBlackboard blackboard, HashSet<EntityUid> entities, UtilityQueryFilter filter)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        switch (filter)
        {
            default:
                throw new NotImplementedException();
        }
    }
}

public readonly record struct UtilityResult(Dictionary<EntityUid, float> Entities)
{
    public static readonly UtilityResult Empty = new();

    public readonly Dictionary<EntityUid, float> Entities = Entities;

    /// <summary>
    /// Returns the entity with the highest score.
    /// </summary>
    public EntityUid? GetHighest()
    {
        return Entities.MaxBy(x => x.Value).Key;
    }

    /// <summary>
    /// Returns the entity with the lowest score. This does not consider entities with a 0 (invalid) score.
    /// </summary>
    public EntityUid? GetLowest()
    {
        return Entities.MinBy(x => x.Value).Key;
    }
}
