using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Conditions;

/// <summary>
/// A condition to check that an entity is placed against a wall (e.g. for light fixtures and surveillance cameras)
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class AgainstWall : IConstructionCondition
{
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var lookupSys = entManager.System<EntityLookupSystem>();
        var tagSys = entManager.System<TagSystem>();

        var againstLocation = new EntityCoordinates(location.EntityId, location.Position - direction.ToVec()); // Subtracting direction: moving backwards relative to placement.

        foreach (var entity in lookupSys.GetEntitiesIntersecting(againstLocation, LookupFlags.Approximate | LookupFlags.Static))
        {
            if (tagSys.HasTag(entity, WallTag))
                return true;
        }

        return false;
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry()
        {
            Localization = "construction-step-condition-against-wall",
        };
    }
}
