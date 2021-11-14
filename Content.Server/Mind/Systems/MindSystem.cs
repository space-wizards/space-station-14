using System;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.MobState.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Mind.Systems
{
    public class MindSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly SharedGhostSystem _ghosts = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MindComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<MindComponent, RemovedComponentEventArgs>(OnRemove);
        }

        private void OnExamine(EntityUid uid, MindComponent component, ExaminedEvent args)
        {
            if (!component.ShowExamineInfo || !args.IsInDetailsRange)
                return;

            EntityManager.TryGetComponent(uid, out MobStateComponent? state);
            var dead = state != null &&state.IsDead();

            if (component.Mind == null)
            {
                var aliveText =
                    $"[color=purple]{Loc.GetString("comp-mind-examined-catatonic", ("ent", component.Owner))}[/color]";
                var deadText = $"[color=red]{Loc.GetString("comp-mind-examined-dead", ("ent", component.Owner))}[/color]";

                args.PushMarkup(dead ? deadText : aliveText);
            }
            else if (component.Mind?.Session == null)
            {
                if (dead) return;

                var text =
                    $"[color=yellow]{Loc.GetString("comp-mind-examined-ssd", ("ent", component.Owner))}[/color]";

                args.PushMarkup(text);
            }
        }

        private void OnRemove(EntityUid uid, MindComponent component, RemovedComponentEventArgs args)
        {
            // Let's not create ghosts if not in the middle of the round.
            if (_ticker.RunLevel != GameRunLevel.InRound)
                return;
            if (component.Mind == null)
                return;

            var visiting = component.Mind.VisitingEntity;
            if (visiting != null)
            {
                if (visiting.TryGetComponent(out GhostComponent? ghost))
                {
                    _ghosts.SetCanReturnToBody(ghost, false);
                }
                TransferTo(component.Mind, visiting.Uid);
            }
            else if (component.GhostOnShutdown)
            {
                if (!EntityManager.TryGetComponent(uid, out TransformComponent transform))
                    return;
                var spawnPosition = transform.Coordinates;

                // Use a regular timer here because the entity has probably been deleted.
                // wth is this magic?
                Timer.Spawn(0, () =>
                {
                    // Async this so that we don't throw if the grid we're on is being deleted.
                    var gridId = spawnPosition.GetGridId(EntityManager);
                    if (gridId == GridId.Invalid || !_map.GridExists(gridId))
                    {
                        spawnPosition = _ticker.GetObserverSpawnPoint();
                    }

                    var ghost = EntityManager.SpawnEntity("MobObserver", spawnPosition);
                    var ghostComponent = ghost.GetComponent<GhostComponent>();
                    _ghosts.SetCanReturnToBody(ghostComponent, false);

                    if (component.Mind != null)
                    {
                        ghost.Name = component.Mind.CharacterName ?? string.Empty;
                        TransferTo(component.Mind, ghost.Uid);
                    }
                });
            }
        }

        public void RemoveOwningPlayer(Mind mind)
        {
            mind.UserId = null;
        }

        /// <summary>
        ///     True if this Mind is 'sufficiently dead' IC (objectives, endtext).
        ///     Note that this is *IC logic*, it's not necessarily tied to any specific truth.
        ///     "If administrators decide that zombies are dead, this returns true for zombies."
        ///     (Maybe you were looking for the action blocker system?)
        /// </summary>
        public bool IsCharacterDeadIC(Mind mind)
        {
            return IsCharacterDeadPhysically(mind);
        }

        /// <summary>
        ///     True if the OwnedEntity of this mind is physically dead.
        ///     This specific definition, as opposed to CharacterDeadIC, is used to determine if ghosting should allow return.
        /// </summary>
        public bool IsCharacterDeadPhysically(Mind mind)
        {
            // This is written explicitly so that the logic can be understood.
            // But it's also weird and potentially situational.
            // Specific considerations when updating this:
            //  + Does being turned into a borg (if/when implemented) count as dead?
            //    *If not, add specific conditions to users of this property where applicable.*
            //  + Is being transformed into a donut 'dead'?
            //    TODO: Consider changing the way ghost roles work.
            //    Mind is an *IC* mind, therefore ghost takeover is IC revival right now.
            //  + Is it necessary to have a reference to a specific 'mind iteration' to cycle when certain events happen?
            //    (If being a borg or AI counts as dead, then this is highly likely, as it's still the same Mind for practical purposes.)

            // This can be null if they're deleted (spike / brain nom)
            if (mind.OwnedEntity == null)
                return true;
            var targetMobState = mind.OwnedEntity.GetComponentOrNull<MobStateComponent>();
            // This can be null if it's a brain (this happens very often)
            // Brains are the result of gibbing so should definitely count as dead
            if (targetMobState == null)
                return true;
            // They might actually be alive.
            return targetMobState.IsDead();
        }

        /// <summary>
        ///     Transfer this mind's control over to a new entity.
        /// </summary>
        /// <param name="mind">Mind that need to be transferred</param>
        /// <param name="entity">
        ///     The entity to control.
        ///     Can be null, in which case it will simply detach the mind from any entity.
        /// </param>
        /// <param name="ghostCheckOverride">
        ///     If true, skips ghost check for Visiting Entity
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown if <paramref name="entity"/> is already owned by another mind.
        /// </exception>
        public void TransferTo(Mind mind, EntityUid? entity, bool ghostCheckOverride = false)
        {
            MindComponent? component = null;
            var alreadyAttached = false;

            if (entity != null)
            {
                if (!EntityManager.TryGetComponent(entity.Value, out component))
                {
                    component = EntityManager.AddComponent<MindComponent>(entity.Value);
                }
                else if (component.HasMind)
                {
                    _ticker.OnGhostAttempt(component.Mind!, false);
                }

                if (EntityManager.TryGetComponent(entity.Value, out ActorComponent? actor))
                {
                    // Happens when transferring to your currently visited entity.
                    if (actor.PlayerSession != mind.Session)
                    {
                        throw new ArgumentException("Visit target already has a session.", nameof(entity));
                    }

                    alreadyAttached = true;
                }
            }


            if (mind.OwnedComponent != null)
                InternalEjectMind(mind.OwnedComponent.Owner.Uid, mind.OwnedComponent);

            mind.OwnedComponent = component;
            if (mind.OwnedComponent != null)
                InternalAssignMind(mind.OwnedComponent.Owner.Uid, mind, mind.OwnedComponent);

            if (mind.IsVisitingEntity
                && (ghostCheckOverride // to force mind transfer, for example from ControlMobVerb
                    || !mind.VisitingEntity!.TryGetComponent(out GhostComponent? ghostComponent) // visiting entity is not a Ghost
                    || !ghostComponent.CanReturnToBody))  // it is a ghost, but cannot return to body anyway, so it's okay
            {
                mind.VisitingEntity = null;
            }

            // Player is CURRENTLY connected.
            if (mind.Session != null && !alreadyAttached && mind.VisitingEntity == null)
            {
                var ent = entity != null ? EntityManager.GetEntity(entity.Value) : null;
                mind.Session.AttachToEntity(ent);
                Logger.Info($"Session {mind.Session.Name} transferred to entity {entity}.");
            }
        }

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="TransferTo"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        private void InternalAssignMind(EntityUid uid, Mind value, MindComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Mind = value;
            RaiseLocalEvent(uid, new MindAddedMessage());
        }

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="TransferTo"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        private void InternalEjectMind(EntityUid uid, MindComponent? component = null)
        {
            // we don't log failed resolve, because entity
            // could be already deleted. It's ok
            if (!Resolve(uid, ref component, logMissing: false))
                return;
            if (!EntityManager.EntityExists(uid))
                return;

            RaiseLocalEvent(uid, new MindRemovedMessage());
            component.Mind = null;
        }
    }
}
