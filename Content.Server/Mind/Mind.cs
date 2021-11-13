using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Server.Mind.Systems;
using Content.Server.Objectives;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Shared.MobState.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Mind
{
    /// <summary>
    ///     A mind represents the IC "mind" of a player. Stores roles currently.
    /// </summary>
    /// <remarks>
    ///     Think of it like this: if a player is supposed to have their memories,
    ///     their mind follows along.
    ///
    ///     Things such as respawning do not follow, because you're a new character.
    ///     Getting borged, cloned, turned into a catbeast, etc... will keep it following you.
    /// </remarks>
    public sealed class Mind
    {
        /// <summary>
        ///     A set of all the roles this mind has.
        ///     Don't try to modify this directly, use <see cref="RolesSystem"/> instead.
        /// </summary>
        [ViewVariables]
        public readonly HashSet<Role> Roles = new();

        /// <summary>
        ///     A list of all the objectives this mind has.
        ///     Don't try to modify this directly, use <see cref="ObjectivesSystem"/> instead.
        /// </summary>
        [ViewVariables]
        public readonly List<Objective> Objectives = new();

        /// <summary>
        ///     Creates the new mind attached to a specific player session.
        /// </summary>
        /// <param name="userId">The session ID of the owning player.</param>
        public Mind(NetUserId userId)
        {
            UserId = userId;
        }

        // TODO: This session should be able to be changed, probably.
        /// <summary>
        ///     The session ID of the player owning this mind.
        /// </summary>
        [ViewVariables]
        public NetUserId? UserId { get; private set; }

        [ViewVariables]
        public bool IsVisitingEntity => VisitingEntity != null;

        [ViewVariables]
        public IEntity? VisitingEntity = null;

        [ViewVariables] public IEntity? CurrentEntity => VisitingEntity ?? OwnedEntity;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? CharacterName = null;

        /// <summary>
        ///     The time of death for this Mind.
        ///     Can be null - will be null if the Mind is not considered "dead".
        /// </summary>
        [ViewVariables] public TimeSpan? TimeOfDeath { get; set; } = null;

        /// <summary>
        ///     The component currently owned by this mind.
        ///     Can be null.
        /// </summary>
        [ViewVariables]
        public MindComponent? OwnedComponent = null;

        /// <summary>
        ///     The entity currently owned by this mind.
        ///     Can be null.
        /// </summary>
        [ViewVariables]
        public IEntity? OwnedEntity => OwnedComponent?.Owner;

        /// <summary>
        ///     The session of the player owning this mind.
        ///     Can be null, in which case the player is currently not logged in.
        /// </summary>
        [ViewVariables]
        public IPlayerSession? Session
        {
            get
            {
                if (!UserId.HasValue)
                {
                    return null;
                }
                var playerMgr = IoCManager.Resolve<IPlayerManager>();
                playerMgr.TryGetSessionById(UserId.Value, out var ret);
                return ret;
            }
        }

        /// <summary>
        ///     True if this Mind is 'sufficiently dead' IC (objectives, endtext).
        ///     Note that this is *IC logic*, it's not necessarily tied to any specific truth.
        ///     "If administrators decide that zombies are dead, this returns true for zombies."
        ///     (Maybe you were looking for the action blocker system?)
        /// </summary>
        [ViewVariables]
        public bool CharacterDeadIC => CharacterDeadPhysically;

        /// <summary>
        ///     True if the OwnedEntity of this mind is physically dead.
        ///     This specific definition, as opposed to CharacterDeadIC, is used to determine if ghosting should allow return.
        /// </summary>
        [ViewVariables]
        public bool CharacterDeadPhysically
        {
            get
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
                if (OwnedEntity == null)
                    return true;
                var targetMobState = OwnedEntity.GetComponentOrNull<MobStateComponent>();
                // This can be null if it's a brain (this happens very often)
                // Brains are the result of gibbing so should definitely count as dead
                if (targetMobState == null)
                    return true;
                // They might actually be alive.
                return targetMobState.IsDead();
            }
        }

        public bool HasRole<T>() where T : Role
        {
            var t = typeof(T);

            return Roles.Any(role => role.GetType() == t);
        }

        public void RemoveOwningPlayer()
        {
            UserId = null;
        }


        public void Visit(IEntity entity)
        {
            Session?.AttachToEntity(entity);
            VisitingEntity = entity;

            var comp = entity.AddComponent<VisitingMindComponent>();
            comp.Mind = this;

            Logger.Info($"Session {Session?.Name} visiting entity {entity}.");
        }

        public void UnVisit()
        {
            if (!IsVisitingEntity)
            {
                return;
            }

            Session?.AttachToEntity(OwnedEntity);
            var oldVisitingEnt = VisitingEntity;
            // Null this before removing the component to avoid any infinite loops.
            VisitingEntity = null;

            DebugTools.AssertNotNull(oldVisitingEnt);

            if (oldVisitingEnt!.HasComponent<VisitingMindComponent>())
            {
                oldVisitingEnt.RemoveComponent<VisitingMindComponent>();
            }

            oldVisitingEnt.EntityManager.EventBus.RaiseLocalEvent(oldVisitingEnt.Uid, new MindUnvisitedMessage());
        }

        public bool TryGetSession([NotNullWhen(true)] out IPlayerSession? session)
        {
            return (session = Session) != null;
        }
    }
}
