using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;

namespace Content.Shared.Shuttles.Systems;

public abstract class SharedDockingSystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem XformSystem = default!;

    public const float DockingHiglightRange = 4f;
    public const float DockRange = 1f + 0.2f;
    public static readonly double AlignmentTolerance = Angle.FromDegrees(15).Theta;

    public bool CanShuttleDock(EntityUid? shuttle)
    {
        if (shuttle == null)
            return false;

        return !HasComp<PreventPilotComponent>(shuttle.Value);
    }

    public bool CanShuttleUndock(EntityUid? shuttle)
    {
        if (shuttle == null)
            return false;

        return !HasComp<PreventPilotComponent>(shuttle.Value);
    }

    public bool CanDock(MapCoordinates mapPosA, Angle worldRotA,
                        MapCoordinates mapPosB, Angle worldRotB)
    {
        // Uh oh
        if (mapPosA.MapId != mapPosB.MapId)
            return false;

        return InRange(mapPosA, mapPosB) && InAlignment(mapPosA, worldRotA, mapPosB, worldRotB);
    }

    public bool InRange(MapCoordinates mapPosA, MapCoordinates mapPosB)
    {
        return (mapPosA.Position - mapPosB.Position).Length() <= DockRange;
    }

    public bool InAlignment(MapCoordinates mapPosA, Angle worldRotA, MapCoordinates mapPosB, Angle worldRotB)
    {
        // Check if the nubs are in line with the two docks.
        var worldRotToB = (mapPosB.Position - mapPosA.Position).ToWorldAngle();
        var worldRotToA = (mapPosA.Position - mapPosB.Position).ToWorldAngle();

        var aDiff = Angle.ShortestDistance((worldRotA - worldRotToB).Reduced(), Angle.Zero);
        var bDiff = Angle.ShortestDistance((worldRotB - worldRotToA).Reduced(), Angle.Zero);

        if (Math.Abs(aDiff.Theta) > AlignmentTolerance)
            return false;

        if (Math.Abs(bDiff.Theta) > AlignmentTolerance)
            return false;

        return true;
    }

    public bool CanDock(NetCoordinates coordinatesOne, Angle angleOne,
                        NetCoordinates coordinatesTwo, Angle angleTwo)
    {
        // TODO: Dump the dock fixtures
        var coordsA = GetCoordinates(coordinatesOne);
        var coordsB = GetCoordinates(coordinatesTwo);

        var mapPosA = XformSystem.ToMapCoordinates(coordsA);
        var mapPosB = XformSystem.ToMapCoordinates(coordsB);

        var worldRotA = XformSystem.GetWorldRotation(coordsA.EntityId) + angleOne;
        var worldRotB = XformSystem.GetWorldRotation(coordsB.EntityId) + angleTwo;

        return CanDock(mapPosA, worldRotA, mapPosB, worldRotB);
    }
}
