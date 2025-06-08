using System.Linq;
using Content.Shared.Construction;
using Robust.Shared.Map;

namespace Content.Shared.Random.Rules.TileRules;

/// <summary>
/// Rule for whether a window can be placed on a specific tile, based on whether an
///     entity on that tiles has  a<see cref="SharedCanBuildWindowOnTopComponent"/>.
/// </summary>
public sealed partial class TileSupportsWindowsRule : TileRule
{
    public override bool TakesIntersecting => true;

    public override bool Check(EntityManager entManager, EntityUid tileParentUid, TileRef tile, Vector2i position, HashSet<EntityUid> intersectingEntities)
        => intersectingEntities.Any(entManager.HasComponent<SharedCanBuildWindowOnTopComponent>) ^ Inverted;
}
