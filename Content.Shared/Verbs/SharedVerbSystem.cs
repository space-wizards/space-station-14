using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Verbs
{
    public class SharedVerbSystem : EntitySystem
    {
        [Dependency] private readonly IEntityLookup _lookup = default!;

        /// <summary>
        ///     Get all of the entities in an area for displaying on the context menu.
        /// </summary>
        /// <param name="buffer">Whether we should slightly extend the entity search area.</param>
        public bool TryGetEntityMenuEntities(IEntity player, MapCoordinates targetPos,
            [NotNullWhen(true)] out List<IEntity>? menuEntities, MenuVisibility visibility, bool buffer = false, bool includeInventory = false)
        {
            menuEntities = null;

            // Check if we have LOS to the clicked-location.
            if ((visibility & MenuVisibility.NoFoV) == 0 &&
                !player.InRangeUnOccluded(targetPos, range: ExamineSystemShared.ExamineRange))
                return false;

            // Get entities
            var length = buffer ? 1.0f : 0.5f;
            var entities = _lookup.GetEntitiesIntersecting(
                    targetPos.MapId,
                    Box2.CenteredAround(targetPos.Position, (length, length)))
                .ToList();

            if (entities.Count == 0)
                return false;

            if (visibility == MenuVisibility.All)
            {
                menuEntities = entities;
                return true;
            }

            // remove any entities in containers
            if ((visibility & MenuVisibility.InContainer) == 0)
            {
                foreach (var entity in entities.ToList())
                {
                    if (!player.IsInSameOrTransparentContainer(entity, userSeeInsideSelf: includeInventory))
                        entities.Remove(entity);
                }
            }

            // remove any invisible entities
            if ((visibility & MenuVisibility.Invisible) == 0)
            {
                foreach (var entity in entities.ToList())
                {
                    if (entity.HasTag("HideContextMenu"))
                        entities.Remove(entity);
                }
            }

            // Remove any entities that do not have LOS
            if ((visibility & MenuVisibility.NoFoV) == 0)
            {
                var playerPos = player.Transform.MapPosition;
                foreach (var entity in entities.ToList())
                {
                    if (!ExamineSystemShared.InRangeUnOccluded(
                        playerPos,
                        entity.Transform.MapPosition,
                        ExamineSystemShared.ExamineRange,
                        null))
                    {
                        entities.Remove(entity);
                    }
                }
            }

            if (entities.Count == 0)
                return false;

            menuEntities = entities;
            return true;
        }

        /// <summary>
        ///     Raises a number of events in order to get all verbs of the given type(s) defined in local systems. This
        ///     does not request verbs from the server.
        /// </summary>
        public virtual Dictionary<VerbType, SortedSet<Verb>> GetLocalVerbs(IEntity target, IEntity user, VerbType verbTypes)
        {
            Dictionary<VerbType, SortedSet<Verb>> verbs = new();

            if ((verbTypes & VerbType.Interaction) == VerbType.Interaction)
            {
                GetInteractionVerbsEvent getVerbEvent = new(user, target);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Interaction, getVerbEvent.Verbs);
            }

            if ((verbTypes & VerbType.Activation) == VerbType.Activation)
            {
                GetActivationVerbsEvent getVerbEvent = new(user, target);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Activation, getVerbEvent.Verbs);
            }

            if ((verbTypes & VerbType.Alternative) == VerbType.Alternative)
            {
                GetAlternativeVerbsEvent getVerbEvent = new(user, target);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Alternative, getVerbEvent.Verbs);
            }

            if ((verbTypes & VerbType.Other) == VerbType.Other)
            {
                GetOtherVerbsEvent getVerbEvent = new(user, target);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Other, getVerbEvent.Verbs);
            }

            return verbs;
        }

        /// <summary>
        ///     Execute the provided verb.
        /// </summary>
        /// <remarks>
        ///     This will try to call the action delegates and raise the local events for the given verb.
        /// </remarks>
        public void ExecuteVerb(Verb verb)
        {

            verb.Act?.Invoke();

            // Maybe raise a local event
            if (verb.ExecutionEventArgs != null)
            {
                if (verb.EventTarget.IsValid())
                    RaiseLocalEvent(verb.EventTarget, verb.ExecutionEventArgs);
                else
                    RaiseLocalEvent(verb.ExecutionEventArgs);
            }
        }
    }

    [Flags, Serializable]
    public enum MenuVisibility
    {
        // What entities can a user see on the entity menu?
        Default = 0,          // They can only see entities in FoV.
        NoFoV = 1 << 0,         // They ignore FoV restrictions
        InContainer = 1 << 1,   // They can see through containers.
        Invisible = 1 << 2,   // They can see entities without sprites and the "HideContextMenu" tag is ignored.
        All = NoFoV | InContainer | Invisible
    }
}
