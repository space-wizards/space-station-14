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
    public class ExplosiveComponent : Component, ITimerTrigger, IDestroyAct
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


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref DamageMax, "damageMax", 1);
            serializer.DataField(ref DamageFalloff, "damageFalloffCoeff", 1);
            serializer.DataField(ref RangeDamageMax, "rangeDamageMax", 1);
            serializer.DataField(ref Range, "range", 1);
        }

        private bool Explosion()
        {
            //Entity damage calculation
            var entitiesAll = _serverEntityManager.GetEntitiesInRange(Owner.Transform.GridPosition, Range).ToList();

            foreach (var entity in entitiesAll)
            {
                var distanceFromEntity = (int)entity.Transform.GridPosition.Distance(_mapManager, Owner.Transform.GridPosition);
                if (!entity.Transform.IsMapTransform)
                    continue;
                if (entity.TryGetComponent(out DamageableComponent damagecomponent))
                {
                    var finalEntityDamage = DamageMax - (DamageFalloff * (distanceFromEntity - RangeDamageMax) * (distanceFromEntity - RangeDamageMax));
                    if (distanceFromEntity <= RangeDamageMax)
                    {
                        finalEntityDamage = DamageMax;
                    }
                    else if (finalEntityDamage < 0)
                    {
                        finalEntityDamage = 0;
                    }
                    damagecomponent.TakeDamage(DamageType.Brute, finalEntityDamage);
                }
            }

            //Tile damage calculation mockup
            //TODO: make it into some sort of actual damage component or whatever the boys think is appropriate
            var mapGrid = _mapManager.GetGrid(Owner.Transform.GridPosition.GridID);
            var circle = new Circle(Owner.Transform.GridPosition.Position, Range);
            var tiles = mapGrid.GetTilesIntersecting(circle);
            foreach (var tile in tiles)
            {
                var tileLoc = mapGrid.GridTileToLocal(tile.GridIndices);
                var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];
                var distanceFromTile = (int)tileLoc.Distance(_mapManager, Owner.Transform.GridPosition);
                if (tileDef.Hardness != 0)
                {
                    var finalTileDamage = DamageMax - (DamageFalloff * (distanceFromTile - RangeDamageMax) * (distanceFromTile - RangeDamageMax)); ;
                    if (distanceFromTile <= RangeDamageMax)
                    {
                        finalTileDamage = DamageMax;
                    }
                    else if (finalTileDamage < 0)
                    {
                        finalTileDamage = 0;
                    }
                    var resultingHardness = tileDef.Hardness - finalTileDamage;
                    if (resultingHardness <= 0)
                    {
                        if (!string.IsNullOrWhiteSpace(tileDef.SubFloor))
                        {
                            var newTileDef = (ContentTileDefinition)_tileDefinitionManager[tileDef.SubFloor];
                            if (newTileDef.Hardness < -resultingHardness)
                            {
                                mapGrid.SetTile(tileLoc, new Tile(_tileDefinitionManager["space"].TileId));
                            }
                            else
                            {
                                mapGrid.SetTile(tileLoc, new Tile(newTileDef.TileId));
                            }
                        }
                        else
                        {
                            mapGrid.SetTile(tileLoc, new Tile(_tileDefinitionManager["space"].TileId));
                        }
                    }
                }
            }

            //Effects and sounds
            var time = IoCManager.Resolve<IGameTiming>().CurTime;
            var message = new EffectSystemMessage
            {
                EffectSprite = "Effects/explosion.png",
                Born = time,
                DeathTime = time + TimeSpan.FromSeconds(5),
                Size = new Vector2(Range / 2, Range / 2),
                Coordinates = Owner.Transform.GridPosition,
                //Rotated from east facing
                Rotation = 0f,
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), 0.5f),
                Shaded = false
            };
            _entitySystemManager.GetEntitySystem<EffectSystem>().CreateParticle(message);
            _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/effects/explosion.ogg", Owner);

            Owner.Delete();
            return true;
        }

        bool ITimerTrigger.Trigger(TimerTriggerEventArgs eventArgs)
        {
            return Explosion();
        }

        void IDestroyAct.Destroy(DestructionEventArgs eventArgs)
        {
            Explosion();
        }
    }
}
