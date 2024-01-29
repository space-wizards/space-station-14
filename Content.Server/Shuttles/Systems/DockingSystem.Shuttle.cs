using System.Linq;
using System.Numerics;
using Content.Server.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class DockingSystem
{
    /*
     * Handles the shuttle side of FTL docking.
     */

    private const int DockRoundingDigits = 2;

    public Angle GetAngle(EntityUid uid, TransformComponent xform, EntityUid targetUid, TransformComponent targetXform, EntityQuery<TransformComponent> xformQuery)
   {
       var (shuttlePos, shuttleRot) = _transform.GetWorldPositionRotation(xform);
       var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetXform);

       var shuttleCOM = Robust.Shared.Physics.Transform.Mul(new Transform(shuttlePos, shuttleRot),
           _physicsQuery.GetComponent(uid).LocalCenter);
       var targetCOM = Robust.Shared.Physics.Transform.Mul(new Transform(targetPos, targetRot),
           _physicsQuery.GetComponent(targetUid).LocalCenter);

       var mapDiff = shuttleCOM - targetCOM;
       var angle = mapDiff.ToWorldAngle();
       angle -= targetRot;
       return angle;
   }

   /// <summary>
   /// Checks if 2 docks can be connected by moving the shuttle directly onto docks.
   /// </summary>
   private bool CanDock(
       DockingComponent shuttleDock,
       TransformComponent shuttleDockXform,
       DockingComponent gridDock,
       TransformComponent gridDockXform,
       Box2 shuttleAABB,
       Angle targetGridRotation,
       FixturesComponent shuttleFixtures,
       MapGridComponent grid,
       bool isMap,
       out Matrix3 matty,
       out Box2 shuttleDockedAABB,
       out Angle gridRotation)
   {
       shuttleDockedAABB = Box2.UnitCentered;
       gridRotation = Angle.Zero;
       matty = Matrix3.Identity;

       if (shuttleDock.Docked ||
           gridDock.Docked ||
           !shuttleDockXform.Anchored ||
           !gridDockXform.Anchored)
       {
           return false;
       }

       // First, get the station dock's position relative to the shuttle, this is where we rotate it around
       var stationDockPos = shuttleDockXform.LocalPosition +
                            shuttleDockXform.LocalRotation.RotateVec(new Vector2(0f, -1f));

       // Need to invert the grid's angle.
       var shuttleDockAngle = shuttleDockXform.LocalRotation;
       var gridDockAngle = gridDockXform.LocalRotation.Opposite();
       var offsetAngle = gridDockAngle - shuttleDockAngle;

       var stationDockMatrix = Matrix3.CreateInverseTransform(stationDockPos, shuttleDockAngle);
       var gridXformMatrix = Matrix3.CreateTransform(gridDockXform.LocalPosition, gridDockAngle);
       Matrix3.Multiply(in stationDockMatrix, in gridXformMatrix, out matty);

       if (!ValidSpawn(grid, matty, offsetAngle, shuttleFixtures, isMap))
           return false;

       shuttleDockedAABB = matty.TransformBox(shuttleAABB);
       gridRotation = (targetGridRotation + offsetAngle).Reduced();
       return true;
   }

   /// <summary>
   /// Gets docking config between 2 specific docks.
   /// </summary>
   public DockingConfig? GetDockingConfig(
       EntityUid shuttleUid,
       EntityUid targetGrid,
       EntityUid shuttleDockUid,
       DockingComponent shuttleDock,
       EntityUid gridDockUid,
       DockingComponent gridDock)
   {
       var shuttleDocks = new List<(EntityUid, DockingComponent)>(1)
       {
           (shuttleDockUid, shuttleDock)
       };

       var gridDocks = new List<(EntityUid, DockingComponent)>(1)
       {
           (gridDockUid, gridDock)
       };

       return GetDockingConfigPrivate(shuttleUid, targetGrid, shuttleDocks, gridDocks);
   }

   /// <summary>
   /// Tries to get a valid docking configuration for the shuttle to the target grid.
   /// </summary>
   /// <param name="priorityTag">Priority docking tag to prefer, e.g. for emergency shuttle</param>
   public DockingConfig? GetDockingConfig(EntityUid shuttleUid, EntityUid targetGrid, string? priorityTag = null)
   {
       var gridDocks = GetDocks(targetGrid);
       var shuttleDocks = GetDocks(shuttleUid);

       return GetDockingConfigPrivate(shuttleUid, targetGrid, shuttleDocks, gridDocks, priorityTag);
   }

   /// <summary>
   /// Tries to get a docking config at the specified coordinates and angle.
   /// </summary>
   public DockingConfig? GetDockingConfigAt(EntityUid shuttleUid,
       EntityUid targetGrid,
       EntityCoordinates coordinates,
       Angle angle)
   {
       var gridDocks = GetDocks(targetGrid);
       var shuttleDocks = GetDocks(shuttleUid);

       var configs = GetDockingConfigs(shuttleUid, targetGrid, shuttleDocks, gridDocks);

       foreach (var config in configs)
       {
           if (config.Coordinates.Equals(coordinates) && config.Angle.EqualsApprox(angle, 0.01))
           {
               return config;
           }
       }

       return null;
   }

   /// <summary>
   /// Gets all docking configs between the 2 grids.
   /// </summary>
   private List<DockingConfig> GetDockingConfigs(
       EntityUid shuttleUid,
       EntityUid targetGrid,
       List<(EntityUid, DockingComponent)> shuttleDocks,
       List<(EntityUid, DockingComponent)> gridDocks)
   {
       var validDockConfigs = new List<DockingConfig>();

       if (gridDocks.Count <= 0)
            return validDockConfigs;

        var targetGridGrid = _gridQuery.GetComponent(targetGrid);
        var targetGridXform = _xformQuery.GetComponent(targetGrid);
        var targetGridAngle = _transform.GetWorldRotation(targetGridXform).Reduced();
        var shuttleFixturesComp = Comp<FixturesComponent>(shuttleUid);
        var shuttleAABB = _gridQuery.GetComponent(shuttleUid).LocalAABB;

        var isMap = HasComp<MapComponent>(targetGrid);

        var grids = new List<Entity<MapGridComponent>>();
        if (shuttleDocks.Count > 0)
        {
           // We'll try all combinations of shuttle docks and see which one is most suitable
           foreach (var (dockUid, shuttleDock) in shuttleDocks)
           {
               var shuttleDockXform = _xformQuery.GetComponent(dockUid);

               foreach (var (gridDockUid, gridDock) in gridDocks)
               {
                   var gridXform = _xformQuery.GetComponent(gridDockUid);

                   if (!CanDock(
                           shuttleDock, shuttleDockXform,
                           gridDock, gridXform,
                           shuttleAABB,
                           targetGridAngle,
                           shuttleFixturesComp,
                           targetGridGrid,
                           isMap,
                           out var matty,
                           out var dockedAABB,
                           out var targetAngle))
                   {
                       continue;
                   }

                   // Can't just use the AABB as we want to get bounds as tight as possible.
                   var gridPosition = new EntityCoordinates(targetGrid, matty.Transform(Vector2.Zero));
                   var spawnPosition = new EntityCoordinates(targetGridXform.MapUid!.Value, gridPosition.ToMapPos(EntityManager, _transform));

                   // TODO: use tight bounds
                   var dockedBounds = new Box2Rotated(shuttleAABB.Translated(spawnPosition.Position), targetAngle, spawnPosition.Position);

                   // Check if there's no intersecting grids (AKA oh god it's docking at cargo).
                   grids.Clear();
                   _mapManager.FindGridsIntersecting(targetGridXform.MapID, dockedBounds, ref grids, includeMap: false);
                   if (grids.Any(o => o.Owner != targetGrid && o.Owner != targetGridXform.MapUid))
                   {
                       continue;
                   }

                   // Alright well the spawn is valid now to check how many we can connect
                   // Get the matrix for each shuttle dock and test it against the grid docks to see
                   // if the connected position / direction matches.

                   var dockedPorts = new List<(EntityUid DockAUid, EntityUid DockBUid, DockingComponent DockA, DockingComponent DockB)>()
                   {
                       (dockUid, gridDockUid, shuttleDock, gridDock),
                   };

                   dockedAABB = dockedAABB.Rounded(DockRoundingDigits);

                   foreach (var (otherUid, other) in shuttleDocks)
                   {
                       if (other == shuttleDock)
                           continue;

                       foreach (var (otherGridUid, otherGrid) in gridDocks)
                       {
                           if (otherGrid == gridDock)
                               continue;

                           if (!CanDock(
                                   other,
                                   _xformQuery.GetComponent(otherUid),
                                   otherGrid,
                                   _xformQuery.GetComponent(otherGridUid),
                                   shuttleAABB,
                                   targetGridAngle,
                                   shuttleFixturesComp, targetGridGrid,
                                   isMap,
                                   out _,
                                   out var otherdockedAABB,
                                   out var otherTargetAngle))
                           {
                               continue;
                           }

                           otherdockedAABB = otherdockedAABB.Rounded(DockRoundingDigits);

                           // Different setup.
                           if (!targetAngle.Equals(otherTargetAngle) ||
                               !dockedAABB.Equals(otherdockedAABB))
                           {
                               continue;
                           }

                           dockedPorts.Add((otherUid, otherGridUid, other, otherGrid));
                       }
                   }

                   validDockConfigs.Add(new DockingConfig()
                   {
                       Docks = dockedPorts,
                       Coordinates = gridPosition,
                       Area = dockedAABB,
                       Angle = targetAngle,
                   });
               }
           }
        }

        return validDockConfigs;
   }

   private DockingConfig? GetDockingConfigPrivate(
       EntityUid shuttleUid,
       EntityUid targetGrid,
       List<(EntityUid, DockingComponent)> shuttleDocks,
       List<(EntityUid, DockingComponent)> gridDocks,
       string? priorityTag = null)
   {
       var validDockConfigs = GetDockingConfigs(shuttleUid, targetGrid, shuttleDocks, gridDocks);

        if (validDockConfigs.Count <= 0)
            return null;

        var targetGridAngle = _transform.GetWorldRotation(targetGrid).Reduced();

        // Prioritise by priority docks, then by maximum connected ports, then by most similar angle.
        validDockConfigs = validDockConfigs
           .OrderByDescending(x => x.Docks.Any(docks =>
               TryComp<PriorityDockComponent>(docks.DockBUid, out var priority) &&
               priority.Tag?.Equals(priorityTag) == true))
           .ThenByDescending(x => x.Docks.Count)
           .ThenBy(x => Math.Abs(Angle.ShortestDistance(x.Angle.Reduced(), targetGridAngle).Theta)).ToList();

        var location = validDockConfigs.First();
        location.TargetGrid = targetGrid;
        // TODO: Ideally do a hyperspace warpin, just have it run on like a 10 second timer.

        return location;
    }

   /// <summary>
   /// Checks whether the shuttle can warp to the specified position.
   /// </summary>
   private bool ValidSpawn(MapGridComponent grid, Matrix3 matty, Angle angle, FixturesComponent shuttleFixturesComp, bool isMap)
   {
       var transform = new Transform(matty.Transform(Vector2.Zero), angle);

       // Because some docking bounds are tight af need to check each chunk individually
       foreach (var fix in shuttleFixturesComp.Fixtures.Values)
       {
           var polyShape = (PolygonShape) fix.Shape;
           var aabb = polyShape.ComputeAABB(transform, 0);
           aabb = aabb.Enlarged(-0.01f);

           // If it's a map check no hard collidable anchored entities overlap
           if (isMap)
           {
               foreach (var tile in grid.GetLocalTilesIntersecting(aabb))
               {
                   var anchoredEnumerator = grid.GetAnchoredEntitiesEnumerator(tile.GridIndices);

                   while (anchoredEnumerator.MoveNext(out var anc))
                   {
                       if (!_physicsQuery.TryGetComponent(anc, out var physics) ||
                           !physics.CanCollide ||
                           !physics.Hard)
                       {
                           continue;
                       }

                       return false;
                   }
               }
           }
           // If it's not a map check it doesn't overlap the grid.
           else
           {
               if (grid.GetLocalTilesIntersecting(aabb).Any())
                   return false;
           }
       }

       return true;
   }

   public List<(EntityUid Uid, DockingComponent Component)> GetDocks(EntityUid uid)
   {
       var result = new List<(EntityUid Uid, DockingComponent Component)>();
       var query = AllEntityQuery<DockingComponent, TransformComponent>();

       while (query.MoveNext(out var dockUid, out var dock, out var xform))
       {
           if (xform.ParentUid != uid || !dock.Enabled)
               continue;

           result.Add((dockUid, dock));
       }

       return result;
   }
}
