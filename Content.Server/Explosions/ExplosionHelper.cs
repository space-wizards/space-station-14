using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Explosion;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Explosions
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
        private static readonly float LightBreakChance = 0.5f;
        private static readonly float HeavyBreakChance = 0.8f;

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
            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();
            var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();

            var exAct = entitySystemManager.GetEntitySystem<ActSystem>();
            
            var entitiesInRange = serverEntityManager.GetEntitiesInRange(mapId, boundingBox, maxRange).ToList();

            foreach (var entity in entitiesInRange)
            {
                if (entity.Deleted || !entity.Transform.IsMapTransform)
                {
                    continue;
                }

                if (!entity.Transform.Coordinates.TryDistance(entityManager, epicenter, out var distance))
                {
                    continue;
                }

                var severity = CalculateSeverity(distance, devastationRange, heaveyRange);
                exAct.HandleExplosion(epicenter, entity, severity);
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

            _ = grid.TryGetComponent(out GridAtmosphereComponent atmosphereComponent);

            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();

            var tilesInGridAndCircle = mapGrid.GetTilesIntersecting(boundingBox);

            foreach (var tile in tilesInGridAndCircle)
            {
                var tileLoc = mapGrid.GridTileToLocal(tile.GridIndices);
                if (!tileLoc.TryDistance(entityManager, epicenter, out var distance) || distance > maxRange)
                {
                    continue;
                }

                if (atmosphereComponent != null && atmosphereComponent.IsAirBlocked(tile.GridIndices))
                {
                    continue;
                }

                var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.Tile.TypeId];
                var baseTurfs = tileDef.BaseTurfs;
                if (baseTurfs.Count == 0)
                {
                    continue;
                }

                var zeroTile = new Tile(tileDefinitionManager[baseTurfs[0]].TileId);
                var previousTile = new Tile(tileDefinitionManager[baseTurfs[^1]].TileId);

                var severity = CalculateSeverity(distance, devastationRange, heaveyRange);

                switch (severity)
                {
                    case ExplosionSeverity.Light:
                        mapGrid.SetTile(tileLoc, previousTile);
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
                if (player.AttachedEntity == null || !player.AttachedEntity.TryGetComponent(out CameraRecoilComponent recoil))
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

        private static void Detonate(IEntity source, int devastationRange, int heavyImpactRange, int lightImpactRange, int flashRange)
        {
            var mapId = source.Transform.MapID;
            if (mapId == MapId.Nullspace)
            {
                return;
            }

            var maxRange = MathHelper.Max(devastationRange, heavyImpactRange, lightImpactRange, 0);

            var epicenter = source.Transform.Coordinates;
            if (source.TryGetContainer(out var container) && container.Owner.HasComponent<EntityStorageComponent>())
            {
                epicenter = container.Owner.Transform.Coordinates;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();
            var boundingBox = Box2.CenteredAround(epicenter.ToMapPos(entityManager), new Vector2(maxRange * 2, maxRange * 2));

            DamageEntitiesInRange(epicenter, boundingBox, devastationRange, heavyImpactRange, maxRange, mapId);

            var mapGridsNear = mapManager.FindGridsIntersecting(mapId, boundingBox);
            foreach (var gridId in mapGridsNear)
            {
                DamageTilesInRange(epicenter, gridId.Index, boundingBox, devastationRange, heavyImpactRange, maxRange);
            }

            CameraShakeInRange(epicenter, maxRange);
            FlashInRange(epicenter, flashRange);
        }

        public static void SpawnExplosion(this IEntity entity, int devastationRange = 0, int heavyImpactRange = 0, int lightImpactRange = 0, int flashRange = 0)
        {
            // If you want to directly set off the explosive
            if (!entity.Deleted && entity.TryGetComponent(out ExplosiveComponent explosive) && !explosive.Exploding)
            {
                explosive.Explosion();
            }
            else
            {
                Detonate(entity, devastationRange, heavyImpactRange, lightImpactRange, flashRange);
            }
        }
    }
}
