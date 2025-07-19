using Content.Shared.Maps;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Construction.Conditions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class TileNotBlocked : IConstructionCondition
{
    [DataField("filterMobs")] private bool _filterMobs = false;
    [DataField("failIfSpace")] private bool _failIfSpace = true;
    [DataField("failIfNotSturdy")] private bool _failIfNotSturdy = true;

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        if (!IoCManager.Resolve<IEntityManager>().TrySystem<TurfSystem>(out var turfSystem))
            return false;

        if (!turfSystem.TryGetTileRef(location, out var tileRef))
        {
            return false;
        }

        if (turfSystem.IsSpace(tileRef.Value) && _failIfSpace)
        {
            return false;
        }

        if (!turfSystem.GetContentTileDefinition(tileRef.Value).Sturdy && _failIfNotSturdy)
        {
            return false;
        }

        return !turfSystem.IsTileBlocked(tileRef.Value, _filterMobs ? CollisionGroup.MobMask : CollisionGroup.Impassable);
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "construction-step-condition-tile-not-blocked",
        };
    }
}
