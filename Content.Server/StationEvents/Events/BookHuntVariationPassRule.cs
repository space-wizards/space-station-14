using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.StationEvents.Events;

public sealed class BookHuntVariationPassRule : StationEventSystem<BookHuntVariationPassRuleComponent>
{
    [Dependency] private readonly StorageSystem _storageSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;
    //[Dependency] private readonly IAdminLogManager _adminLogger = default!;

    protected override void Started(EntityUid uid, BookHuntVariationPassRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);
        //Randomize what books are selected
        RobustRandom.Shuffle<EntProtoId>(comp.BookPrototypes);

        //The first book is hidden in a smuggler's satchel under the floor tiles
        if (TryFindRandomTile(out _, out _, out _, out var coords) && 0 < comp.BookPrototypes.Count)
        {
            var satchel = Spawn(comp.SmugglerSatchel, coords);
            var book = Spawn(comp.BookPrototypes[0], MapCoordinates.Nullspace);
            Sawmill.Info($"Book Hunt is spawning {satchel} and {book} at {coords}");
            if (!TryComp<StorageComponent>(satchel, out var storage) || !_storageSystem.Insert(satchel, book, out _, null, storage, false))
            {
                Del(satchel);
                Del(book);
            }
        }

        if (TryGetRandomStation(out var station))
        {
            //The second book is hidden on a random shelf. This is probably the easiest to find for the librarian.
            if (1 < comp.BookPrototypes.Count)
            {
                SpawnBookInContainer(comp, station, 1, comp.BookshelfTag);
            }

            //The third book is hidden in a random dresser, making it quite likely to be in a head's room
            if (2 < comp.BookPrototypes.Count)
            {
                SpawnBookInContainer(comp, station, 2, comp.DresserTag);
            }

            //The fourth book is hidden in a random suit storage container
            if (3 < comp.BookPrototypes.Count)
            {
                var validSuit = new List<(EntityUid, EntityStorageComponent)>();
                var spawn = Spawn(comp.BookPrototypes[3], MapCoordinates.Nullspace);

                var query = EntityQueryEnumerator<EntityStorageComponent, TransformComponent, TagComponent>();
                while (query.MoveNext(out var ent, out var storage, out var xform, out _))
                {
                    if (StationSystem.GetOwningStation(ent, xform) != station)
                        continue;

                    if (!_entityStorageSystem.CanInsert(spawn, ent, storage))
                        continue;

                    if (!_tagSystem.HasTag(ent, comp.SuitStorageTag))
                        continue;

                    validSuit.Add((ent, storage));
                }

                if (validSuit.Count > 0)
                {
                    var (suit, storage) = RobustRandom.Pick(validSuit);
                    Sawmill.Info($"Book Hunt is inserting {spawn} into {suit}.");
                    if (!_entityStorageSystem.Insert(spawn, suit, storage))
                    {
                        Del(spawn);
                    }
                }
                else
                {
                    Del(spawn);
                }
            }

            //The fifth book is hidden in a duffle bag in space, close to the station
            if (4 < comp.BookPrototypes.Count)
            {
                var gridUid = StationSystem.GetLargestGrid(station.Value);
                if (gridUid != null && TryComp<MapGridComponent>(gridUid, out var grid))
                {
                    //Figure out its AABB size and use that as a guide to how far the initial point should be
                    var size = grid.LocalAABB.Size.Length() / 2;
                    var distance = size + comp.SpawnBuffer;
                    var angle = RobustRandom.NextAngle();
                    //Position relative to station center
                    var location = angle.ToVec() * distance;

                    //Find the initial point. NOTE: Dev has a wonky origin point, so it isn't so great for testing this
                    var xform = Transform(gridUid.Value);
                    var position = _transform.GetWorldPosition(xform) + location;

                    //Shoot backwards towards the station and catch the first collision
                    var ray = new CollisionRay(position, angle.Opposite().ToVec(), (int)comp.CollisionMask);
                    var rayCastResults = _physics.IntersectRay(xform.MapID, ray, distance, null, false).ToList();

                    if (rayCastResults.Count > 0)
                    {
                        //Adjust the duffel back slightly toward the initial point
                        var adjust = angle.ToVec() * comp.HitDistance;
                        var position2 = _transform.GetWorldPosition(rayCastResults[0].HitEntity) + adjust;
                        var coords2 = new MapCoordinates(position2, xform.MapID);

                        var duffel = Spawn(comp.Duffel, coords2);
                        var book = Spawn(comp.BookPrototypes[4], MapCoordinates.Nullspace);
                        Sawmill.Info($"Book Hunt hit {rayCastResults[0].HitEntity} and picked the location {coords2} in space for {duffel} and {book}.");

                        if (!TryComp<StorageComponent>(duffel, out var storage) || !_storageSystem.Insert(duffel, book, out _, null, storage, false))
                        {
                            Del(duffel);
                            Del(book);
                        }
                    }
                }
            }
        }
    }

    private void SpawnBookInContainer(BookHuntVariationPassRuleComponent comp, EntityUid? station, int bookIndex, ProtoId<TagPrototype> tag)
    {
        //The second book is hidden in a random dresser, making it quite likely to be in a head's room
        if (bookIndex < comp.BookPrototypes.Count)
        {
            var validContainer = new List<(EntityUid, StorageComponent)>();
            var spawn = Spawn(comp.BookPrototypes[bookIndex], MapCoordinates.Nullspace);

            var query = EntityQueryEnumerator<StorageComponent, TransformComponent, TagComponent>();
            while (query.MoveNext(out var ent, out var storage, out var xform, out _))
            {
                if (StationSystem.GetOwningStation(ent, xform) != station)
                    continue;

                if (!_storageSystem.CanInsert(ent, spawn, out _, storage))
                    continue;

                if (!_tagSystem.HasTag(ent, tag))
                    continue;

                validContainer.Add((ent, storage));
            }

            if (validContainer.Count > 0)
            {
                var (container, storage) = RobustRandom.Pick(validContainer);
                Sawmill.Info($"Book Hunt is inserting {spawn} into {container}.");
                if (!_storageSystem.Insert(container, spawn, out _, null, storage, false))
                {
                    Del(spawn);
                }
            }
            else
            {
                Del(spawn);
            }
        }
    }
}
