#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.GameObjects.Verbs
{
    public class SharedVerbSystem : EntitySystem
    {
        private SharedInteractionSystem _interactionSystem = null!;

        public override void Initialize()
        {
            base.Initialize();
            _interactionSystem = Get<SharedInteractionSystem>();
        }

        /// <summary>
        ///     Get all of the entities relevant for the contextmenu
        /// </summary>
        /// <param name="player"></param>
        /// <param name="targetPos"></param>
        /// <param name="contextEntities"></param>
        /// <param name="buffer">Whether we should slightly extend out the ignored range for the ray predicated</param>
        /// <returns></returns>
        protected bool TryGetContextEntities(IEntity player, MapCoordinates targetPos, [NotNullWhen(true)] out List<IEntity>? contextEntities, bool buffer = false)
        {
            contextEntities = null;
            var length = buffer ? 1.0f: 0.5f;
            
            var entities = EntityManager.GetEntitiesIntersecting(targetPos.MapId,
                Box2.CenteredAround(targetPos.Position, (length, length))).ToList();
            
            if (entities.Count == 0)
            {
                return false;
            }
            
            // Check if we have LOS to the clicked-location, otherwise no popup.
            var vectorDiff = player.Transform.MapPosition.Position - targetPos.Position;
            var distance = vectorDiff.Length + 0.01f;
            Func<IEntity, bool> ignored = entity => entities.Contains(entity) || 
                                                    entity == player || 
                                                    !entity.TryGetComponent(out OccluderComponent? occluder) ||
                                                    !occluder.Enabled;

            var result = _interactionSystem.InRangeUnobstructed(
                player.Transform.MapPosition, 
                targetPos,
                distance, 
                (int) CollisionGroup.Opaque, 
                ignored);

            if (!result)
            {
                return false;
            }

            contextEntities = entities;
            return true;
        }
    }
}