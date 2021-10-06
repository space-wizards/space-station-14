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
            [NotNullWhen(true)] out List<IEntity>? menuEntities, bool buffer = false, bool showAll = false, bool ignoreFoV = false, bool includeInventory = false)
        {
            menuEntities = null;

            // Check if we have LOS to the clicked-location.
            if (!(showAll || ignoreFoV) && !player.InRangeUnOccluded(targetPos, range: ExamineSystemShared.ExamineRange))
                return false;

            // Get entities
            var length = buffer ? 1.0f : 0.5f;
            var entities = _lookup.GetEntitiesIntersecting(
                    targetPos.MapId,
                    Box2.CenteredAround(targetPos.Position, (length, length)))
                .ToList();

            if (entities.Count == 0)
                return false;

            if (showAll)
            {
                menuEntities = entities;
                return true;
            }

            // We are not showing all entities. Remove any that should not appear on the menu. This includes both
            // entities that just should not show up, and entities that are currently inside of other containers.
            player.TryGetContainer(out var playerContainer);
            foreach (var entity in entities.ToList())
            {
                if (entity.HasTag("HideContextMenu") ||
                    !CanSeeContainerCheck(entity, player, playerContainer, includeInventory))
                    entities.Remove(entity);
            }

            if (entities.Count == 0)
                return false;

            if (ignoreFoV)
            {
                menuEntities = entities;
                return true;
            }

            // We are not ignoring FoV (aka, this is not a ghost/spectator). Perform visibility checks.
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

            if (entities.Count == 0)
                return false;

            menuEntities = entities;
            return true;
        }

        /// <summary>
        ///     Can the player see the entity through any entity containers?
        /// </summary>
        /// <remarks>
        ///     This is similar to <see cref="ContainerHelpers.IsInSameOrParentContainer()"/>, except that we do not
        ///     allow the player to be the "parent" container and we allow for see-through containers (display cases).
        ///     If we allowed the player to be the parent container, they could see their own organs.
        /// </remarks>
        private bool CanSeeContainerCheck(IEntity entity, IEntity player, IContainer? playerContainer, bool includeInventory)
        {
            // is the player inside this entity?
            if (playerContainer?.Owner == entity)
                return true;

            entity.TryGetContainer(out var entityContainer);

            // IS the player the container that this entity is in? Usually we want to exclude those entities (organs
            // should not appear in the entity menu). But we need to allow this when the user is right-clicking on
            // inventory slots.
            if (includeInventory && entityContainer?.Owner == player)
                return true;

            // are they in the same container (or none?)
            if (playerContainer == entityContainer)
                return true;

            if (entityContainer == null)
                return false;

            // Is the entity in a display case / see-through container?
            entityContainer.Owner.TryGetContainer(out var parentContainer);
            if (entityContainer.ShowContents && playerContainer == parentContainer)
                return true;

            return false;
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
}
