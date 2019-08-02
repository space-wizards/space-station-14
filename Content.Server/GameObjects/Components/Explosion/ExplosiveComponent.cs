using System;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Explosive
{
    [RegisterComponent]
    public class ExplosiveComponent : Component, ITimerTrigger, IDestroyAct
    {
#pragma warning disable 649
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerEntityManager _serverEntityManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        public override string Name => "Explosive";

        public int DevastationRange = 0;
        public int HeavyImpactRange = 0;
        public int LightImpactRange = 0;
        public int FlashRange = 0;


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref DevastationRange, "devastationRange", 0);
            serializer.DataField(ref HeavyImpactRange, "heavyImpactRange", 0);
            serializer.DataField(ref LightImpactRange, "lightImpactRange", 0);
            serializer.DataField(ref FlashRange, "flashRange", 0);
        }

        private bool Explosion()
        {
            var maxRange = MathHelper.Max(DevastationRange, HeavyImpactRange, LightImpactRange, 0f);
            //Entity damage calculation
            var entitiesAll = _serverEntityManager.GetEntitiesInRange(Owner.Transform.GridPosition, maxRange).ToList();

            foreach (var entity in entitiesAll)
            {
                Owner.Delete();
                if (entity == Owner)
                    continue;
                if (!entity.Transform.IsMapTransform)
                    continue;
                var distanceFromEntity = (int)entity.Transform.GridPosition.Distance(_mapManager, Owner.Transform.GridPosition);
                var exAct = _entitySystemManager.GetEntitySystem<ActSystem>();
                var severity = ExplosionSeverity.Destruction;
                if (distanceFromEntity < DevastationRange)
                {
                    severity = ExplosionSeverity.Destruction;
                }
                else if (distanceFromEntity < HeavyImpactRange)
                {
                    severity = ExplosionSeverity.Heavy;
                }
                else if (distanceFromEntity < LightImpactRange)
                {
                    severity = ExplosionSeverity.Light;
                }
                else
                {
                    continue;
                }
                exAct.HandleExplosion(Owner, entity, severity);
            }

            //Tile damage calculation mockup
            //TODO: make it into some sort of actual damage component or whatever the boys think is appropriate
            var mapGrid = _mapManager.GetGrid(Owner.Transform.GridPosition.GridID);
            var circle = new Circle(Owner.Transform.GridPosition.Position, maxRange);
            var tiles = mapGrid.GetTilesIntersecting(circle);
            foreach (var tile in tiles)
            {
                var tileLoc = mapGrid.GridTileToLocal(tile.GridIndices);
                var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];
                var distanceFromTile = (int)tileLoc.Distance(_mapManager, Owner.Transform.GridPosition);
                if (!string.IsNullOrWhiteSpace(tileDef.SubFloor)) {
                    if (distanceFromTile < DevastationRange)
                        mapGrid.SetTile(tileLoc, new Tile(_tileDefinitionManager["space"].TileId));
                    if (distanceFromTile < HeavyImpactRange)
                    {
                        if (new Random().Prob(80))
                        {
                            mapGrid.SetTile(tileLoc, new Tile(_tileDefinitionManager[tileDef.SubFloor].TileId));
                        }
                        else
                        {
                            mapGrid.SetTile(tileLoc, new Tile(_tileDefinitionManager["space"].TileId));
                        }
                    }
                    if (distanceFromTile < LightImpactRange)
                    {
                        if (new Random().Prob(50))
                        {
                            mapGrid.SetTile(tileLoc, new Tile(_tileDefinitionManager[tileDef.SubFloor].TileId));
                        }
                    }
                }
            }

            //Effects and sounds
            var time = IoCManager.Resolve<IGameTiming>().CurTime;
            var message = new EffectSystemMessage
            {
                EffectSprite = "Effects/explosion.rsi",
                RsiState = "explosionfast",
                Born = time,
                DeathTime = time + TimeSpan.FromSeconds(5),
                Size = new Vector2(FlashRange / 2, FlashRange / 2),
                Coordinates = Owner.Transform.GridPosition,
                //Rotated from east facing
                Rotation = 0f,
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), 0.5f),
                Shaded = false
            };
            _entitySystemManager.GetEntitySystem<EffectSystem>().CreateParticle(message);
            _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/effects/explosion.ogg", Owner);

            // Knock back cameras of all players in the area.

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var selfPos = Owner.Transform.WorldPosition;
            foreach (var player in playerManager.GetAllPlayers())
            {
                if (player.AttachedEntity == null
                    || player.AttachedEntity.Transform.MapID != mapGrid.ParentMapId
                    || !player.AttachedEntity.TryGetComponent(out CameraRecoilComponent recoil))
                {
                    continue;
                }

                var playerPos = player.AttachedEntity.Transform.WorldPosition;
                var delta = selfPos - playerPos;
                var distance = delta.LengthSquared;

                var effect = 1 / (1 + 0.2f * distance);
                if (effect > 0.01f)
                {
                    var kick = -delta.Normalized * effect;
                    recoil.Kick(kick);
                }
            }

            return true;
        }

        bool ITimerTrigger.Trigger(TimerTriggerEventArgs eventArgs)
        {
            return Explosion();
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            Explosion();
        }
    }
}
