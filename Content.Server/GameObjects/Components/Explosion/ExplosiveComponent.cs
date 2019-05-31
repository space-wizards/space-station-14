using System;
using System.Linq;
using System.Collections.Generic;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.Maps;

namespace Content.Server.GameObjects.Components.Explosive
{
    public class ExplosiveComponent : Component, ITimerTrigger
    {
#pragma warning disable 649        
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerEntityManager _serverEntityManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        public override string Name => "Explosive";

        public int DamageMax = 1;
        public int DamageFalloff = 1;
        public int RangeDamageMax = 1;
        public int Range = 1;
        public int Delay = 1;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref DamageMax, "damageMax", 1);
            serializer.DataField(ref DamageFalloff, "damageFalloff", 1);
            serializer.DataField(ref RangeDamageMax, "rangeDamageMax", 1);
            serializer.DataField(ref Range, "range", 1);
        }

        bool ITimerTrigger.Trigger(TimerTriggerEventArgs eventArgs)
        {
            var location = eventArgs.Source.Transform.GridPosition;

            var entitiesAll = _serverEntityManager.GetEntitiesInRange(location, Range);

            List<IEntity> entities = entitiesAll.ToList();

            foreach (var entity in entitiesAll)
            {
                var distance = (int)entity.Transform.GridPosition.Distance(_mapManager, location);
                if (!entity.Transform.IsMapTransform || distance > RangeDamageMax)
                    continue;
                if (entity.TryGetComponent(out DamageableComponent damagecomponent))
                {
                    damagecomponent.TakeDamage(DamageType.Brute, DamageMax);
                }
            }

            foreach (var entity in entities)
            {
                var distance = (int)entity.Transform.GridPosition.Distance(_mapManager, location);
                if (!entity.Transform.IsMapTransform || distance <= RangeDamageMax)
                    continue;

                if (entity.TryGetComponent(out DamageableComponent damagecomponent))
                {
                    
                    var overallDamageFalloff = DamageFalloff;
                    if (overallDamageFalloff > DamageMax) {
                        overallDamageFalloff = DamageMax;
                    }
                    damagecomponent.TakeDamage(DamageType.Brute, DamageMax - distance*overallDamageFalloff);
                }
            }

            var mapGrid = _mapManager.GetGrid(location.GridID);
            //var circle = new Circle(location.Position, Range);
            var aabb = new Box2(location.Position - new Vector2(Range / 2, Range / 2), location.Position + new Vector2(Range / 2, Range / 2));
            var tiles = mapGrid.GetTilesIntersecting(aabb);
            foreach (var tile in tiles)
            {
                var tileLoc = new GridCoordinates(tile.X, tile.Y, tile.GridIndex);
                var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];
                var distanceFromTile = tileLoc.Distance(_mapManager, location);
                if (distanceFromTile > RangeDamageMax)
                {
                    var underplating = _tileDefinitionManager["underplating"];
                    mapGrid.SetTile(tileLoc, new Tile(underplating.TileId));
                } 
                else
                {
                    var space = _tileDefinitionManager["space"];
                    mapGrid.SetTile(tileLoc, new Tile(space.TileId));
                }
            }
            var time = IoCManager.Resolve<IGameTiming>().CurTime;
            var message = new EffectSystemMessage
            {
                EffectSprite = "Effects/explosion.png",
                Born = time,
                DeathTime = time + TimeSpan.FromSeconds(5),
                Size = new Vector2(Range / 2, Range / 2),
                Coordinates = location,
                //Rotated from east facing
                Rotation = 0f,
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), 0.5f),
                Shaded = false
            };
            _entitySystemManager.GetEntitySystem<EffectSystem>().CreateParticle(message);
            _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/effects/explosion.ogg", eventArgs.Source);

            Owner.Delete();
            return true;
        }
    }
}
