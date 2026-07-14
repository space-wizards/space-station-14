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
    private static readonly ProtoId<TagPrototype> DiagonalTag = "Diagonal";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    /// <summary>
    /// The angle that the wall must be from the entity's direction, in degrees.
    /// Defaults to 180 degrees (when placed against a wall to the south, the entity should be facing north)
    /// </summary>
    [DataField("offset")] private Angle _offset = Angle.FromDegrees(180);

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var lookupSys = entManager.System<EntityLookupSystem>();
        var tagSys = entManager.System<TagSystem>();

        var offsetDirection = (direction.ToAngle() + _offset).GetCardinalDir();

        var againstLocation = new EntityCoordinates(location.EntityId, location.Position + offsetDirection.ToVec());

        foreach (var entity in lookupSys.GetEntitiesIntersecting(againstLocation, LookupFlags.Approximate | LookupFlags.Static))
        {
            if (!tagSys.HasTag(entity, WallTag))
                continue;

            if (tagSys.HasTag(entity, DiagonalTag)
                && entManager.TryGetComponent(entity, out TransformComponent? xform))
            {
                // In a south facing, diagonal walls have flat sides only to the south and east.
                // If we're attaching from the north or from the west, we cancel that.
                var wallDir = xform.LocalRotation.GetCardinalDir();
                if (wallDir == offsetDirection
                    || wallDir == offsetDirection.GetClockwise90Degrees())
                    continue;
            }

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
