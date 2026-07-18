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
    /// The angle to add to the direction of the entity to point it towards the wall.
    /// Defaults to 180 degrees - lights, for example, should face north when attached to a wall to the south.
    /// </summary>
    [DataField("offset")] private Angle _offset = Angle.FromDegrees(180);

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        var lookupSys = entManager.System<EntityLookupSystem>();
        var tagSys = entManager.System<TagSystem>();

        var towardsWall = (direction.ToAngle() + _offset).GetCardinalDir();

        var againstLocation = new EntityCoordinates(location.EntityId, location.Position + towardsWall.ToVec());

        foreach (var entity in lookupSys.GetEntitiesIntersecting(againstLocation, LookupFlags.Approximate | LookupFlags.Static))
        {
            if (!tagSys.HasTag(entity, WallTag))
                continue;

            if (tagSys.HasTag(entity, DiagonalTag)
                && entManager.TryGetComponent(entity, out TransformComponent? xform))
            {
                // In a south facing, diagonal walls have flat sides only to the south and east.
                // If we're attaching to the north or west side (so towardsWall points south or east), we cancel that.
                var wallDir = xform.LocalRotation.GetCardinalDir();
                if (wallDir == towardsWall // South check
                    || wallDir == towardsWall.GetClockwise90Degrees()) // East check
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
