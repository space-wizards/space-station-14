using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Explosion;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
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

        private static ExplosionSeverity CalculateSeverity(in float distance, in float devastationRange, in float heaveyRange)
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

        private static void DamageEntitiesAndTiles(this EntityCoordinates epicenter, in float devastationRange, in float heaveyRange, in float maxrange)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryGetGrid(epicenter.GetGridId(entityManager), out var mapGrid))
            {
                return;
            }

            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();
            var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            var exAct = entitySystemManager.GetEntitySystem<ActSystem>();
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();

            var alive_entities_airtight = new HashSet<Vector2i>();
            var entities_in = serverEntityManager.GetEntitiesInRange(epicenter, maxrange * 2).ToList();
            foreach (var entity in entities_in)
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

                if (!entity.Deleted && entity.TryGetComponent(out GameObjects.Components.Atmos.AirtightComponent airtight) && airtight.AirBlocked)
                {
                    // This should stop tiles from being affected by the explosion if
                    // the wall on top of them were not destroyed.
                    alive_entities_airtight.Add(entity.Transform.Coordinates.ToVector2i(entityManager, mapManager));
                }
            }

            var tiles_in = mapGrid.GetTilesIntersecting(new Circle(epicenter.ToMapPos(entityManager), maxrange));
            foreach (var tile in tiles_in)
            {
                if (alive_entities_airtight.Contains(tile.GridIndices))
                {
                    continue;
                }

                var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.Tile.TypeId];
                var baseTurfs = tileDef.BaseTurfs;
                if (baseTurfs.Count == 0)
                {
                    continue;
                }

                var tileLoc = mapGrid.GridTileToLocal(tile.GridIndices);
                if (!tileLoc.TryDistance(entityManager, epicenter, out var distance))
                {
                    continue;
                }

                var severity = CalculateSeverity(distance, devastationRange, heaveyRange);
                var zeroTile = new Tile(tileDefinitionManager[baseTurfs[0]].TileId);
                var previousTile = new Tile(tileDefinitionManager[baseTurfs[^1]].TileId);

                switch (severity)
                {
                    case ExplosionSeverity.Light:
                        mapGrid.SetTile(tileLoc, previousTile);
                        if (!previousTile.IsEmpty && robustRandom.Prob(0.5f))
                        {
                            mapGrid.SetTile(tileLoc, previousTile);
                        }
                        break;
                    case ExplosionSeverity.Heavy:
                        if (!previousTile.IsEmpty && robustRandom.Prob(0.8f))
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

        private static void CameraShakeInRange(this EntityCoordinates epicenter, in float maxrange)
        {
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var players = playerManager.GetPlayersInRange(epicenter, (int) Math.Ceiling(maxrange));
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
                    delta = (0.1f, 0.1f);

                var distance = delta.LengthSquared;
                var effect = 10 * (1 / (1 + distance));
                if (effect > 0.01f)
                {
                    var kick = -delta.Normalized * effect;
                    recoil.Kick(kick);
                }
            }
        }

        private static void FlashInRange(this EntityCoordinates epicenter, in float flashrange)
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

        public static void SpawnExplosion(this IEntity source, int devastationRange = 0, int heavyImpactRange = 0, int lightImpactRange = 0, int flashRange = 0)
        {
            // If you want to directly set off the explosive
            if (!source.Deleted && source.TryGetComponent(out ExplosiveComponent explosive) && !explosive.Exploding)
            {
                explosive.Explosion();
            }
            else
            {
                SpawnExplosion(source.Transform.Coordinates, devastationRange, heavyImpactRange, lightImpactRange, flashRange);
            }
        }

        public static void SpawnExplosion(this EntityCoordinates epicenter, int devastationRange, int heavyImpactRange, int lightImpactRange, int flashRange)
        {
            var maxrange = MathHelper.Max(devastationRange, heavyImpactRange, lightImpactRange, 0);

            epicenter.DamageEntitiesAndTiles(devastationRange, heavyImpactRange, maxrange);
            epicenter.CameraShakeInRange(maxrange);
            epicenter.FlashInRange(flashRange);

            var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            entitySystemManager.GetEntitySystem<AudioSystem>().PlayAtCoords("/Audio/Effects/explosion.ogg", epicenter);
        }
    }
}
