using Robust.Shared.Map;

namespace Content.Shared.Shuttles.Systems;

public abstract class SharedDockingSystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem XformSystem = default!;

    public const float DockingHiglightRange = 4f;
    public const float DockRange = 1f + 0.2f;
    public static readonly Angle AlignmentTolerance = Angle.FromDegrees(15);

    public bool CanDock(NetCoordinates coordinatesOne, Angle angleOne,
                        NetCoordinates coordinatesTwo, Angle angleTwo)
    {
        // TODO: Dump the dock fixtures
        var coordsA = GetCoordinates(coordinatesOne);
        var coordsB = GetCoordinates(coordinatesTwo);

        var mapPosA = XformSystem.ToMapCoordinates(coordsA);
        var mapPosB = XformSystem.ToMapCoordinates(coordsB);

        // Uh oh
        if (mapPosA.MapId != mapPosB.MapId)
        {
            return false;
        }

        // Check range
        if ((mapPosA.Position - mapPosB.Position).Length() > DockRange)
        {
            return false;
        }

        // Check if the nubs are in line with the two docks.
        var worldRotA = XformSystem.GetWorldRotation(coordsA.EntityId) + angleOne;
        var worldRotB = XformSystem.GetWorldRotation(coordsB.EntityId) + angleTwo;

        var worldRotToB = (mapPosB.Position - mapPosA.Position).ToWorldAngle();
        var worldRotToA = (mapPosA.Position - mapPosB.Position).ToWorldAngle();

        var aDiff = (worldRotA - worldRotToB).Reduced();
        var bDiff = (worldRotB - worldRotToA).Reduced();

        if (Math.Abs(aDiff.Theta) > AlignmentTolerance.Theta)
        {
            return false;
        }

        if (Math.Abs(bDiff.Theta) > AlignmentTolerance.Theta)
        {
            return false;
        }

        return true;
    }
}
