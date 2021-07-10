#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Camera;
using Content.Server.Explosion.Components;
using Content.Shared.Acts;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Sound;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Explosion
{
    public static class ExplosionHelper
    {
        /// <summary>
        /// Distance used for camera shake when distance from explosion is (0.0, 0.0).
        /// Avoids getting NaN values down the line from doing math on (0.0, 0.0).
        /// </summary>
        private static readonly Vector2 EpicenterDistance = (0.1f, 0.1f);

        /// <summary>
        /// Chance of a tile breaking if the severity is Light and Heavy
        /// </summary>
        private static readonly float LightBreakChance = 0.3f;
        private static readonly float HeavyBreakChance = 0.8f;
        private static SoundSpecifier _explosionSound = new SoundPathSpecifier("/Audio/Effects/explosion.ogg");

        private static bool IgnoreExplosivePassable(IEntity e) => e.HasTag("ExplosivePassable");

        private static ExplosionSeverity CalculateSeverity(float distance, float devastationRange, float heaveyRange)
        {
            if (distance < devastationRange)
            {
                return ExplosionSeverity.Destruction;
            }
            else if (distance < heaveyRange)
            {
                return ExplosionSeverity.Heavy;
            }
            else
            {
                return ExplosionSeverity.Light;
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
        private static void DamageEntitiesInRange(EntityCoordinates epicenter, Box2 boundingBox,
                                                                    float devastationRange,
                                                                    float heaveyRange,
                                                                    float maxRange,
                                                                    MapId mapId)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var exAct = EntitySystem.Get<ActSystem>();

            var entitiesInRange = IoCManager.Resolve<IEntityLookup>().GetEntitiesInRange(mapId, boundingBox, 0).ToList();

            var impassableEntities = new List<Tuple<IEntity, float>>();
            var nonImpassableEntities = new List<Tuple<IEntity, float>>();
            // TODO: Given this seems to rely on physics it should just query directly like everything else.

            // The entities are paired with their distance to the epicenter
            // and splitted into two lists based on if they are Impassable or not
            foreach (var entity in entitiesInRange)
            {
                if (entity.Deleted || !entity.Transform.IsMapTransform)
                {
                    continue;
                }

                if (!entity.Transform.Coordinates.TryDistance(entityManager, epicenter, out var distance) || distance > maxRange)
                {
                    continue;
                }

                if (!entity.TryGetComponent(out PhysicsComponent? body) || body.Fixtures.Count < 1)
                {
                    continue;
                }

                if ((body.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                {
                    impassableEntities.Add(Tuple.Create(entity, distance));
                }
                else
                {
                    nonImpassableEntities.Add(Tuple.Create(entity, distance));
                }
            }

            // The Impassable entities are sorted in descending order
            // Entities closer to the epicenter are first
            impassableEntities.Sort((x, y) => x.Item2.CompareTo(y.Item2));

            // Impassable entities are handled first. If they are damaged enough, they are destroyed and they may
            // be able to spawn a new entity. I.e Wall -> Girder.
            // Girder has a tag ExplosivePassable, and the predicate make it so the entities with this tag are ignored
            var epicenterMapPos = epicenter.ToMap(entityManager);
            foreach (var (entity, distance) in impassableEntities)
            {
                if (!entity.InRangeUnobstructed(epicenterMapPos, maxRange, ignoreInsideBlocker: true, predicate: IgnoreExplosivePassable))
                {
                    continue;
                }
                exAct.HandleExplosion(epicenter, entity, CalculateSeverity(distance, devastationRange, heaveyRange));
            }

            // Impassable entities were handled first so NonImpassable entities have a bigger chance to get hit. As now
            // there are probably more ExplosivePassable entities around
            foreach (var (entity, distance) in nonImpassableEntities)
            {
                if (!entity.InRangeUnobstructed(epicenterMapPos, maxRange, ignoreInsideBlocker: true, predicate: IgnoreExplosivePassable))
                {
                    continue;
                }
                exAct.HandleExplosion(epicenter, entity, CalculateSeverity(distance, devastationRange, heaveyRange));
            }
        }

        /// <summary>
        /// Damage tiles inside the range. The type of tile can change depending on a discrete
        /// damage bracket [light, heavy, devastation], the distance from the epicenter and
        /// a probabilty bracket [<see cref="LightBreakChance"/>, <see cref="HeavyBreakChance"/>, 1.0].
        /// </summary>
        ///
        private static void DamageTilesInRange(EntityCoordinates epicenter,
                                               GridId gridId,
                                               Box2 boundingBox,
                                               float devastationRange,
                                               float heaveyRange,
                                               float maxRange)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryGetGrid(gridId, out var mapGrid))
            {
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (!entityManager.TryGetEntity(mapGrid.GridEntityId, out var grid))
            {
                return;
            }

            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();

            var tilesInGridAndCircle = mapGrid.GetTilesIntersecting(boundingBox);

            var epicenterMapPos = epicenter.ToMap(entityManager);
            foreach (var tile in tilesInGridAndCircle)
            {
                var tileLoc = mapGrid.GridTileToLocal(tile.GridIndices);
                if (!tileLoc.TryDistance(entityManager, epicenter, out var distance) || distance > maxRange)
                {
                    continue;
                }

                if (tile.IsBlockedTurf(false))
                {
                    continue;
                }

                if (!tileLoc.ToMap(entityManager).InRangeUnobstructed(epicenterMapPos, maxRange, ignoreInsideBlocker: false, predicate: IgnoreExplosivePassable))
                {
                    continue;
                }

                var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.Tile.TypeId];
                var baseTurfs = tileDef.BaseTurfs;
                if (baseTurfs.Count == 0)
                {
                    continue;
                }

                var zeroTile = new Robust.Shared.Map.Tile(tileDefinitionManager[baseTurfs[0]].TileId);
                var previousTile = new Robust.Shared.Map.Tile(tileDefinitionManager[baseTurfs[^1]].TileId);

                var severity = CalculateSeverity(distance, devastationRange, heaveyRange);

                switch (severity)
                {
                    case ExplosionSeverity.Light:
                        if (!previousTile.IsEmpty && robustRandom.Prob(LightBreakChance))
                        {
                            mapGrid.SetTile(tileLoc, previousTile);
                        }
                        break;
                    case ExplosionSeverity.Heavy:
                        if (!previousTile.IsEmpty && robustRandom.Prob(HeavyBreakChance))
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

        private static void CameraShakeInRange(EntityCoordinates epicenter, float maxRange)
        {
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var players = playerManager.GetPlayersInRange(epicenter, (int) Math.Ceiling(maxRange));
            foreach (var player in players)
            {
                if (player.AttachedEntity == null || !player.AttachedEntity.TryGetComponent(out CameraRecoilComponent? recoil))
                {
                    continue;
                }

                var entityManager = IoCManager.Resolve<IEntityManager>();

                var playerPos = player.AttachedEntity.Transform.WorldPosition;
                var delta = epicenter.ToMapPos(entityManager) - playerPos;

                //Change if zero. Will result in a NaN later breaking camera shake if not changed
                if (delta.EqualsApprox((0.0f, 0.0f)))
                    delta = EpicenterDistance;

                var distance = delta.LengthSquared;
                var effect = 10 * (1 / (1 + distance));
                if (effect > 0.01f)
                {
                    var kick = -delta.Normalized * effect;
                    recoil.Kick(kick);
                }
            }
        }

        private static void FlashInRange(EntityCoordinates epicenter, float flashrange)
        {
            if (flashrange > 0)
            {
                var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
                var time = IoCManager.Resolve<IGameTiming>().CurTime;
                var message = new EffectSystemMessage
                {
                    EffectSprite = "Effects/explosion.rsi",
                    RsiState = "explosionfast",
                    Born = time,
                    DeathTime = time + TimeSpan.FromSeconds(5),
                    Size = new Vector2(flashrange / 2, flashrange / 2),
                    Coordinates = epicenter,
                    Rotation = 0f,
                    ColorDelta = new Vector4(0, 0, 0, -1500f),
                    Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), 0.5f),
                    Shaded = false
                };
                entitySystemManager.GetEntitySystem<EffectSystem>().CreateParticle(message);
            }
        }

        public static void SpawnExplosion(this IEntity entity, int devastationRange = 0, int heavyImpactRange = 0,
            int lightImpactRange = 0, int flashRange = 0)
        {
            // If you want to directly set off the explosive
            if (!entity.Deleted && entity.TryGetComponent(out ExplosiveComponent? explosive) && !explosive.Exploding)
            {
                explosive.Explosion();
            }
            else
            {
                while (entity.TryGetContainer(out var cont))
                {
                    entity = cont.Owner;
                }

                var epicenter = entity.Transform.Coordinates;

                SpawnExplosion(epicenter, devastationRange, heavyImpactRange, lightImpactRange, flashRange);
            }
        }

        public static void SpawnExplosion(EntityCoordinates epicenter, int devastationRange = 0,
            int heavyImpactRange = 0, int lightImpactRange = 0, int flashRange = 0)
        {
            var mapId = epicenter.GetMapId(IoCManager.Resolve<IEntityManager>());
            if (mapId == MapId.Nullspace)
            {
                return;
            }

            var maxRange = MathHelper.Max(devastationRange, heavyImpactRange, lightImpactRange, 0);

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var epicenterMapPos = epicenter.ToMapPos(entityManager);
            var boundingBox = new Box2(epicenterMapPos - new Vector2(maxRange, maxRange),
                epicenterMapPos + new Vector2(maxRange, maxRange));

            if(_explosionSound.TryGetSound(out var explosionSound))
                SoundSystem.Play(Filter.Broadcast(), explosionSound, epicenter);
            DamageEntitiesInRange(epicenter, boundingBox, devastationRange, heavyImpactRange, maxRange, mapId);

            var mapGridsNear = mapManager.FindGridsIntersecting(mapId, boundingBox);

            foreach (var gridId in mapGridsNear)
            {
                DamageTilesInRange(epicenter, gridId.Index, boundingBox, devastationRange, heavyImpactRange, maxRange);
            }

            CameraShakeInRange(epicenter, maxRange);
            FlashInRange(epicenter, flashRange);
        }
    }
}
