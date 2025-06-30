using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Random.Rules.TileRules;


/// <summary>
/// Rules-based conditional selection. Variant of <see cref="RulesPrototype"/>, but is in terms of
///     individual grid tiles, rather than only their entities.
/// </summary>
[Prototype]
public sealed partial class TileRulesPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    [DataField("rules", required: true)]
    public List<TileRule> Rules = new();
}

/// <summary>
/// Variant of <see cref="RulesRule"/>, but is applied to individual grid tiles
///     rather than entities.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class TileRule
{
    /// <summary>Does this rule take intersecting entities as an input when checking it?</summary>
    // This is done so that intersecting entities aren't evaluated for every single TileRule, but only for those that actually need it.
    public virtual bool TakesIntersecting => false;

    [DataField]
    public bool Inverted;

    /// <summary>
    /// Checks whether this rule is valid, based on a tile and it's parent grid or map. If <paramref name="intersectingEntities"/>
    ///     is in use to check if this rule is true, then <see cref="TakesIntersecting"/> should be true, otherwise the set of
    ///     intersecting entities may not have been evaluated.
    /// </summary>
    /// <remarks>Always pure.</remarks>
    /// <param name="tileParentUid">The <see cref="EntityUid"/> (such as map or grid) that contains the specified tile.</param>
    /// <param name="intersectingEntities">
    /// The entities intersecting with the tile, with an enlargement of <see cref="TileRulesSystem.IntersectionOffset"/>.
    ///     Will not be guaranteed to have been evaluated (i.e., it would've stayed empty) unless <see cref="TakesIntersecting"/>
    ///     is true.
    /// </param>
    public abstract bool Check(EntityManager entManager, EntityUid tileParentUid, TileRef tile, Vector2i position, HashSet<EntityUid> intersectingEntities);
}

public sealed class TileRulesSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    // The enlargement used when getting the default intersecting entities of a tile.
    public const float DefaultIntersectionEnlargement = -0.05f;

    /// <summary>
    /// Checks if a given set of <see cref="TileRule"/>s is valid. Optionally takes
    ///     a list of entities intersecting the tile, that should be resolved with
    ///     <see cref="DefaultIntersectionEnlargement"/>.
    /// </summary>
    /// <param name="tileParentUid">The <see cref="EntityUid"/> (such as map or grid) that contains the specified tile.</param>
    /// <seealso cref="TileRule.TakesIntersecting"/>
    public bool IsTrue(EntityUid tileParentUid, TileRulesPrototype rules, TileRef tile, Vector2i position, HashSet<EntityUid>? intersectingEntities = null)
    {
        // If we were already provided with an array of intersecting entities, we don't need to evaluate it again.
        var reEvaluateIntersecting = intersectingEntities == null;
        intersectingEntities ??= new();

        foreach (var rule in rules.Rules)
        {
            // Only if a rule NEEDs a list of intersecting entities -- and if we need to re-evaluate them, then do so.
            if (rule.TakesIntersecting && reEvaluateIntersecting)
            {
                _lookup.GetLocalEntitiesIntersecting(tileParentUid, position, intersectingEntities, DefaultIntersectionEnlargement, LookupFlags.Uncontained);
                reEvaluateIntersecting = false;
            }

            if (!rule.Check(EntityManager, tileParentUid, tile, position, intersectingEntities))
                return false;
        }

        return true;
    }
}
