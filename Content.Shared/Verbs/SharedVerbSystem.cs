using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Verbs
{
    public class SharedVerbSystem : EntitySystem
    {
        /// <summary>
        ///     Get all of the entities relevant for the context menu
        /// </summary>
        /// <param name="player"></param>
        /// <param name="targetPos"></param>
        /// <param name="contextEntities"></param>
        /// <param name="buffer">Whether we should slightly extend out the ignored range for the ray predicated</param>
        /// <returns></returns>
        public bool TryGetContextEntities(IEntity player, MapCoordinates targetPos, [NotNullWhen(true)] out List<IEntity>? contextEntities, bool buffer = false)
        {
            contextEntities = null;
            var length = buffer ? 1.0f : 0.5f;

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

        /// <summary>
        ///     Run the given verb. This will try to call delegates and raises any events. If none of these fields are
        ///     non-null (nothing to do), returns false. True otherwise.
        /// </summary>
        public bool TryExecuteVerb(Verb verb)
        {
            if (verb.Act == null &&
                verb.LocalVerbEventArgs == null &&
                verb.NetworkVerbEventArgs == null)
            {
                // Nothing to do. This verb is probably defined server-side, and the client needs to ask the server to execute this verb.
                return false;
            }

            // Run the delegate
            verb.Act?.Invoke();

            // Raise the local event
            if (verb.LocalVerbEventArgs != null)
            {
                if (verb.LocalEventTarget.IsValid())
                {
                    RaiseLocalEvent(verb.LocalEventTarget, verb.LocalVerbEventArgs);
                }
                else
                {
                    RaiseLocalEvent(verb.LocalVerbEventArgs);
                }
            }

            // Network event
            if (verb.NetworkVerbEventArgs != null)
            {
                RaiseNetworkEvent(verb.NetworkVerbEventArgs);
            }

            return true;
        }
    }
}
