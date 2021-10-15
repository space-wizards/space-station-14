using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Tag;
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
        public bool TryGetContextEntities(IEntity player, MapCoordinates targetPos,
            [NotNullWhen(true)] out List<IEntity>? contextEntities, bool buffer = false, bool ignoreVisibility = false)
        {
            contextEntities = null;

            // Check if we have LOS to the clicked-location.
            if (!ignoreVisibility && !player.InRangeUnOccluded(targetPos, range: ExamineSystemShared.ExamineRange))
                return false;

            // Get entities
            var length = buffer ? 1.0f : 0.5f;
            var entities = _lookup.GetEntitiesIntersecting(
                    targetPos.MapId,
                    Box2.CenteredAround(targetPos.Position, (length, length)))
                .ToList();

            if (entities.Count == 0) return false;

            if (ignoreVisibility)
            {
                contextEntities = entities;
                return true;
            }

            // perform visibility checks
            var playerPos = player.Transform.MapPosition;
            foreach (var entity in entities.ToList())
            {
                if (entity.HasTag("HideContextMenu"))
                {
                    entities.Remove(entity);
                    continue;
                }

                if (!ExamineSystemShared.InRangeUnOccluded(
                        playerPos,
                        entity.Transform.MapPosition,
                        ExamineSystemShared.ExamineRange,
                        null) )
                {
                    entities.Remove(entity);
                }
            }

            if (entities.Count == 0)
                return false;

            contextEntities = entities;
            return true;
        }

        /// <summary>
        ///     Raises a number of events in order to get all verbs of the given type(s)
        /// </summary>
        public Dictionary<VerbType, SortedSet<Verb>> GetVerbs(IEntity target, IEntity user, VerbType verbTypes)
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
        ///     Execute actions associated with the given verb.
        /// </summary>
        /// <remarks>
        ///     This will try to call delegates and raise any events for the given verb.
        /// </remarks>
        public bool TryExecuteVerb(Verb verb)
        {
            var executed = false;

            // Maybe run a delegate
            if (verb.Act != null)
            {
                executed = true;
                verb.Act.Invoke();
            }
            
            // Maybe raise a local event
            if (verb.LocalVerbEventArgs != null)
            {
                executed = true;
                if (verb.LocalEventTarget.IsValid())
                    RaiseLocalEvent(verb.LocalEventTarget, verb.LocalVerbEventArgs);
                else
                    RaiseLocalEvent(verb.LocalVerbEventArgs);
            }

            // maybe raise a network event
            if (verb.NetworkVerbEventArgs != null)
            {
                executed = true;
                RaiseNetworkEvent(verb.NetworkVerbEventArgs);
            }
                
            // return false if all of these were null
            return executed;
        }
    }
}
