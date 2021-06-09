#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.GameObjects.Verbs
{
    public class SharedVerbSystem : EntitySystem
    {
        /// <summary>
        ///     Get all of the entities relevant for the contextmenu
        /// </summary>
        /// <param name="player"></param>
        /// <param name="targetPos"></param>
        /// <param name="contextEntities"></param>
        /// <param name="buffer">Whether we should slightly extend out the ignored range for the ray predicated</param>
        /// <returns></returns>
        public bool TryGetContextEntities(IEntity player, MapCoordinates targetPos, [NotNullWhen(true)] out List<IEntity>? contextEntities, bool buffer = false)
        {
            contextEntities = null;
            var length = buffer ? 1.0f: 0.5f;

            var entities = IoCManager.Resolve<IEntityLookup>().
                GetEntitiesIntersecting(targetPos.MapId, Box2.CenteredAround(targetPos.Position, (length, length))).ToList();

            if (entities.Count == 0)
            {
                return false;
            }

            // Check if we have LOS to the clicked-location, otherwise no popup.
            var vectorDiff = player.Transform.MapPosition.Position - targetPos.Position;
            var distance = vectorDiff.Length + 0.01f;
            bool Ignored(IEntity entity)
            {
                return entities.Contains(entity) ||
                       entity == player ||
                       !entity.TryGetComponent(out OccluderComponent? occluder) ||
                       !occluder.Enabled;
            }

            var mask = player.TryGetComponent(out SharedEyeComponent? eye) && eye.DrawFov
                ? CollisionGroup.Opaque
                : CollisionGroup.None;

            var result = player.InRangeUnobstructed(targetPos, distance, mask, Ignored);

            if (!result)
            {
                return false;
            }

            contextEntities = entities;
            return true;
        }
    }
}
