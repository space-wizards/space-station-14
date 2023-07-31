using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;

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
        internal readonly ISet<Role> Roles = new HashSet<Role>();

        internal readonly List<Objective> Objectives = new();

        public string Briefing = String.Empty;

        /// <summary>
        ///     Creates the new mind.
        ///     Note: the Mind is NOT initially attached!
        ///     The provided UserId is solely for tracking of intended owner.
        /// </summary>
        public Mind()
        {
        }

        /// <summary>
        ///     The session ID of the player owning this mind.
        /// </summary>
        [ViewVariables, Access(typeof(MindSystem))]
        public NetUserId? UserId { get; set; }

        /// <summary>
        ///     The session ID of the original owner, if any.
        ///     May end up used for round-end information (as the owner may have abandoned Mind since)
        /// </summary>
        [ViewVariables, Access(typeof(MindSystem))]
        public NetUserId? OriginalOwnerUserId { get; set; }

        /// <summary>
        ///     Entity UID for the first entity that this mind controlled. Used for round end.
        ///     Might be relevant if the player has ghosted since.
        /// </summary>
        [ViewVariables] public EntityUid? OriginalOwnedEntity;

        [ViewVariables]
        public bool IsVisitingEntity => VisitingEntity != null;

        [ViewVariables, Access(typeof(MindSystem))]
        public EntityUid? VisitingEntity { get; set; }

        [ViewVariables]
        public EntityUid? CurrentEntity => VisitingEntity ?? OwnedEntity;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? CharacterName { get; set; }

        /// <summary>
        ///     The time of death for this Mind.
        ///     Can be null - will be null if the Mind is not considered "dead".
        /// </summary>
        [ViewVariables]
        public TimeSpan? TimeOfDeath { get; set; }

        /// <summary>
        ///     The component currently owned by this mind.
        ///     Can be null.
        /// </summary>
        [ViewVariables]
        public MindContainerComponent? OwnedComponent { get; internal set; }

        /// <summary>
        ///     The entity currently owned by this mind.
        ///     Can be null.
        /// </summary>
        [ViewVariables, Access(typeof(MindSystem))]
        public EntityUid? OwnedEntity { get; set; }

        /// <summary>
        ///     An enumerable over all the roles this mind has.
        /// </summary>
        [ViewVariables]
        public IEnumerable<Role> AllRoles => Roles;

        /// <summary>
        ///     An enumerable over all the objectives this mind has.
        /// </summary>
        [ViewVariables]
        public IEnumerable<Objective> AllObjectives => Objectives;

        /// <summary>
        ///     Prevents user from ghosting out
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("preventGhosting")]
        public bool PreventGhosting { get; set; }

        /// <summary>
        ///     Prevents user from suiciding
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("preventSuicide")]
        public bool PreventSuicide { get; set; }

        /// <summary>
        ///     The session of the player owning this mind.
        ///     Can be null, in which case the player is currently not logged in.
        /// </summary>
        [ViewVariables, Access(typeof(MindSystem), typeof(GameTicker))]
        public IPlayerSession? Session { get; internal set; }

        /// <summary>
        ///     Gets the current job
        /// </summary>
        public Job? CurrentJob => Roles.OfType<Job>().SingleOrDefault();
    }
}
