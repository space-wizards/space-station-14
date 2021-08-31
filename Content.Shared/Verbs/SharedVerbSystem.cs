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

    /// <summary>
    ///     The types of interactions to include when assembling a list of verbs.
    /// </summary>
    /// <remarks> <list type="bullet"> <item>
    ///     <term>Interact verbs</term>
    ///     <description>are those that involve using the hands or the currently held item on some other entity. These
    ///     may be triggered by using left-mouse or 'Z'.</description>
    /// </item> <item>
    ///    <term>Activate verbs</term>
    ///    <description>are those that activate an item in the world. E.g., opening a door or a GUI. These are
    ///    independent of the currently held items and can always be triggered using 'E', but left-mouse or 'Z' may also trigger
    ///    these.</description>
    /// </item> <item>
    ///     <term>Alternative verbs</term>
    ///     <description>are those that can be triggered by using the 'alt' modifier (alt +
    ///     left-mouse/E/Z).</description>
    /// </item> <item>
    ///     <term>Other verbs</term>
    ///     <description>are global interactions like "examine", "pull", or "debug".</description>
    /// </item>  </list> </remarks>
    [Flags]
    public enum VerbTypes : short
    {
        Interact = 1, //   Z/left-mouse or context menu
        Activate = 2, // E/Z/left-mouse or context menu
        Alternative = 4, // alt + E/Z/left-mouse or context menu
        Other = 8 // context menu only
    }
}
