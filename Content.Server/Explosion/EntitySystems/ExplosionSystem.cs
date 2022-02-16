using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Explosion.Components;
using Content.Shared.Acts;
using Content.Shared.Camera;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Sound;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed class ExplosionSystem : EntitySystem
    {
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        /// <summary>
        /// Distance used for camera shake when distance from explosion is (0.0, 0.0).
        /// Avoids getting NaN values down the line from doing math on (0.0, 0.0).
        /// </summary>
        private static readonly Vector2 EpicenterDistance = (0.1f, 0.1f);

        /// <summary>
        /// Chance of a tile breaking if the severity is Light and Heavy
        /// </summary>
        private const float LightBreakChance = 0.3f;
        private const float HeavyBreakChance = 0.8f;

        // TODO move this to the component
        private static readonly SoundSpecifier ExplosionSound = new SoundCollectionSpecifier("explosion");

        [Dependency] private readonly IEntityLookup _entityLookup = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _maps = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ITileDefinitionManager _tiles = default!;

        [Dependency] private readonly ActSystem _acts = default!;
        [Dependency] private readonly EffectSystem _effects = default!;
        [Dependency] private readonly TriggerSystem _triggers = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;
        [Dependency] private readonly CameraRecoilSystem _cameraRecoil = default!;
        [Dependency] private readonly TagSystem _tags = default!;

        private bool IgnoreExplosivePassable(EntityUid e)
        {
            return _tags.HasTag(e, "ExplosivePassable");
        }

        private ExplosionSeverity CalculateSeverity(float distance, float devastationRange, float heavyRange)
        {
            if (distance < devastationRange)
            {
                return ExplosionSeverity.Destruction;
            }
            else if (distance < heavyRange)
            {
                return ExplosionSeverity.Heavy;
            }
            else
            {
                return ExplosionSeverity.Light;
            }
        }

        private void CameraShakeInRange(EntityCoordinates epicenter, float maxRange)
        {
            var players = Filter.Empty()
                .AddInRange(epicenter.ToMap(EntityManager), MathF.Ceiling(maxRange))
                .Recipients;

            foreach (var player in players)
            {
                if (player.AttachedEntity is not {Valid: true} playerEntity ||
                    !EntityManager.HasComponent<CameraRecoilComponent>(playerEntity))
                {
                    continue;
                }

                var playerPos = EntityManager.GetComponent<TransformComponent>(playerEntity).WorldPosition;
                var delta = epicenter.ToMapPos(EntityManager) - playerPos;

                //Change if zero. Will result in a NaN later breaking camera shake if not changed
                if (delta.EqualsApprox((0.0f, 0.0f)))
                    delta = EpicenterDistance;

                var distance = delta.LengthSquared;
                var effect = 10 * (1 / (1 + distance));
                if (effect > 0.01f)
                {
                    var kick = -delta.Normalized * effect;
                    _cameraRecoil.KickCamera(player.AttachedEntity.Value, kick);
                }
            }
        }

        /// <summary>
        /// Damage entities inside the range. The damage depends on a discrete
        /// damage bracket [light, heavy, devastation] and the distance from the epicenter
        /// </summary>
        /// <returns>
        /// A dictionary of coordinates relative to the parents of every grid of entities that survived the explosion,
        /// have an airtight component and are currently blocking air. Like a wall.
        /// </returns>
        private void DamageEntitiesInRange(
            EntityCoordinates epicenter,
            Box2 boundingBox,
            float devastationRange,
            float heavyRange,
            float maxRange,
            MapId mapId)
        {
            var entitiesInRange = _entityLookup.GetEntitiesInRange(mapId, boundingBox, 0).ToList();

            var impassableEntities = new List<(EntityUid, float)>();
            var nonImpassableEntities = new List<(EntityUid, float)>();
            // TODO: Given this seems to rely on physics it should just query directly like everything else.

            // The entities are paired with their distance to the epicenter
            // and splitted into two lists based on if they are Impassable or not
            foreach (var entity in entitiesInRange)
            {
                if (Deleted(entity) || entity.IsInContainer())
                {
                    continue;
                }

                if (!EntityManager.GetComponent<TransformComponent>(entity).Coordinates.TryDistance(EntityManager, epicenter, out var distance) ||
                    distance > maxRange)
                {
                    continue;
                }

                if (!EntityManager.TryGetComponent(entity, out FixturesComponent? fixturesComp) || fixturesComp.Fixtures.Count < 1)
                {
                    continue;
                }

                if (!EntityManager.TryGetComponent(entity, out PhysicsComponent? body))
                {
                    continue;
                }

                if ((body.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                {
                    impassableEntities.Add((entity, distance));
                }
                else
                {
                    nonImpassableEntities.Add((entity, distance));
                }
            }

            // The Impassable entities are sorted in descending order
            // Entities closer to the epicenter are first
            impassableEntities.Sort((x, y) => x.Item2.CompareTo(y.Item2));

            // Impassable entities are handled first. If they are damaged enough, they are destroyed and they may
            // be able to spawn a new entity. I.e Wall -> Girder.
            // Girder has a tag ExplosivePassable, and the predicate make it so the entities with this tag are ignored
            var epicenterMapPos = epicenter.ToMap(EntityManager);
            foreach (var (entity, distance) in impassableEntities)
            {
                if (!_interactionSystem.InRangeUnobstructed(epicenterMapPos, entity, maxRange, predicate: IgnoreExplosivePassable))
                {
                    continue;
                }

                _acts.HandleExplosion(epicenter, entity, CalculateSeverity(distance, devastationRange, heavyRange));
            }

            // Impassable entities were handled first so NonImpassable entities have a bigger chance to get hit. As now
            // there are probably more ExplosivePassable entities around
            foreach (var (entity, distance) in nonImpassableEntities)
            {
                if (!_interactionSystem.InRangeUnobstructed(epicenterMapPos, entity, maxRange, predicate: IgnoreExplosivePassable))
                {
                    continue;
                }

                _acts.HandleExplosion(epicenter, entity, CalculateSeverity(distance, devastationRange, heavyRange));
            }
        }

        /// <summary>
        /// Damage tiles inside the range. The type of tile can change depending on a discrete
        /// damage bracket [light, heavy, devastation], the distance from the epicenter and
        /// a probability bracket [<see cref="LightBreakChance"/>, <see cref="HeavyBreakChance"/>, 1.0].
        /// </summary>
        ///
        private void DamageTilesInRange(EntityCoordinates epicenter,
                                               GridId gridId,
                                               Box2 boundingBox,
                                               float devastationRange,
                                               float heaveyRange,
                                               float maxRange)
        {
            if (!_maps.TryGetGrid(gridId, out var mapGrid))
            {
                return;
            }

            if (!EntityManager.EntityExists(mapGrid.GridEntityId))
            {
                return;
            }

            var tilesInGridAndCircle = mapGrid.GetTilesIntersecting(boundingBox);
            var epicenterMapPos = epicenter.ToMap(EntityManager);

            foreach (var tile in tilesInGridAndCircle)
            {
                var tileLoc = mapGrid.GridTileToLocal(tile.GridIndices);
                if (!tileLoc.TryDistance(EntityManager, epicenter, out var distance) ||
                    distance > maxRange)
                {
                    continue;
                }

                if (tile.IsBlockedTurf(false))
                {
                    continue;
                }

                if (!_interactionSystem.InRangeUnobstructed(tileLoc.ToMap(EntityManager), epicenterMapPos, maxRange, predicate: IgnoreExplosivePassable))
                {
                    continue;
                }

                var tileDef = (ContentTileDefinition) _tiles[tile.Tile.TypeId];
                var baseTurfs = tileDef.BaseTurfs;
                if (baseTurfs.Count == 0)
                {
                    continue;
                }

                var zeroTile = new Tile(_tiles[baseTurfs[0]].TileId);
                var previousTile = new Tile(_tiles[baseTurfs[^1]].TileId);

                var severity = CalculateSeverity(distance, devastationRange, heaveyRange);

                switch (severity)
                {
                    case ExplosionSeverity.Light:
                        if (!previousTile.IsEmpty && _random.Prob(LightBreakChance))
                        {
                            mapGrid.SetTile(tileLoc, previousTile);
                        }
                        break;
                    case ExplosionSeverity.Heavy:
                        if (!previousTile.IsEmpty && _random.Prob(HeavyBreakChance))
                        {
                            mapGrid.SetTile(tileLoc, previousTile);
                        }
                        break;
                    case ExplosionSeverity.Destruction:
                        mapGrid.SetTile(tileLoc, zeroTile);
                        break;
                }
            }
        }

        private void FlashInRange(EntityCoordinates epicenter, float flashRange)
        {
            if (flashRange > 0)
            {
                var time = _timing.CurTime;
                var message = new EffectSystemMessage
                {
                    EffectSprite = "Effects/explosion.rsi",
                    RsiState = "explosionfast",
                    Born = time,
                    DeathTime = time + TimeSpan.FromSeconds(5),
                    Size = new Vector2(flashRange / 2, flashRange / 2),
                    Coordinates = epicenter,
                    Rotation = 0f,
                    ColorDelta = new Vector4(0, 0, 0, -1500f),
                    Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), 0.5f),
                    Shaded = false
                };

                _effects.CreateParticle(message);
            }
        }

        public void SpawnExplosion(
            EntityUid entity,
            int devastationRange = 0,
            int heavyImpactRange = 0,
            int lightImpactRange = 0,
            int flashRange = 0,
            EntityUid? user = null,
            ExplosiveComponent? explosive = null,
            TransformComponent? transform = null)
        {
            if (!Resolve(entity, ref transform))
            {
                return;
            }

            Resolve(entity, ref explosive, false);

            if (explosive is { Exploding: false })
            {
                _triggers.Explode(entity, explosive, user);
            }
            else
            {
                while (EntityManager.EntityExists(entity) && entity.TryGetContainer(out var container))
                {
                    entity = container.Owner;
                }

                if (!EntityManager.TryGetComponent(entity, out transform))
                {
                    return;
                }

                var epicenter = transform.Coordinates;

                SpawnExplosion(epicenter, devastationRange, heavyImpactRange, lightImpactRange, flashRange, entity, user);
            }
        }

        public void SpawnExplosion(
            EntityCoordinates epicenter,
            int devastationRange = 0,
            int heavyImpactRange = 0,
            int lightImpactRange = 0,
            int flashRange = 0,
            EntityUid? entity = null,
            EntityUid? user = null)
        {
            var mapId = epicenter.GetMapId(EntityManager);
            if (mapId == MapId.Nullspace)
            {
                return;
            }

            // logging
            var range = $"{devastationRange}/{heavyImpactRange}/{lightImpactRange}/{flashRange}";
            if (entity == null || !entity.Value.IsValid())
            {
                _logSystem.Add(LogType.Explosion, LogImpact.High, $"Explosion spawned at {epicenter:coordinates} with range {range}");
            }
            else if (user == null || !user.Value.IsValid())
            {
                _logSystem.Add(LogType.Explosion, LogImpact.High,
                    $"{ToPrettyString(entity.Value):entity} exploded at {epicenter:coordinates} with range {range}");
            }
            else
            {
                _logSystem.Add(LogType.Explosion, LogImpact.High,
                    $"{ToPrettyString(user.Value):user} caused {ToPrettyString(entity.Value):entity} to explode at {epicenter:coordinates} with range {range}");
            }

            var maxRange = MathHelper.Max(devastationRange, heavyImpactRange, lightImpactRange, 0);
            var epicenterMapPos = epicenter.ToMapPos(EntityManager);
            var boundingBox = new Box2(epicenterMapPos - new Vector2(maxRange, maxRange),
                epicenterMapPos + new Vector2(maxRange, maxRange));

            SoundSystem.Play(Filter.Broadcast(), ExplosionSound.GetSound(), epicenter);
            DamageEntitiesInRange(epicenter, boundingBox, devastationRange, heavyImpactRange, maxRange, mapId);

            var mapGridsNear = _maps.FindGridsIntersecting(mapId, boundingBox);

            foreach (var gridId in mapGridsNear)
            {
                DamageTilesInRange(epicenter, gridId.Index, boundingBox, devastationRange, heavyImpactRange, maxRange);
            }

            CameraShakeInRange(epicenter, maxRange);
            FlashInRange(epicenter, flashRange);
        }
    }
}
