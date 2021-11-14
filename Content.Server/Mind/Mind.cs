using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Mind.Systems;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.MobState.Components;
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
        // TODO: This session should be able to be changed, probably.
        /// <summary>
        ///     The session ID of the player owning this mind.
        /// </summary>
        [ViewVariables]
        public NetUserId? UserId = null;

        /// <summary>
        ///     IC minds name.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? CharacterName = null;

        /// <summary>
        ///     The time of death for this Mind.
        ///     Can be null - will be null if the Mind is not considered "dead".
        /// </summary>
        [ViewVariables]
        public TimeSpan? TimeOfDeath = null;

        [ViewVariables]
        public IEntity? VisitingEntity = null;

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

        [ViewVariables]
        public bool IsVisitingEntity => VisitingEntity != null;

        [ViewVariables] public IEntity? CurrentEntity => VisitingEntity ?? OwnedEntity;

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

        public bool HasRole<T>() where T : Role
        {
            var t = typeof(T);

            return Roles.Any(role => role.GetType() == t);
        }

        public bool TryGetSession([NotNullWhen(true)] out IPlayerSession? session)
        {
            return (session = Session) != null;
        }
    }
}
