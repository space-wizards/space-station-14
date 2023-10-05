using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Shared.Mind
{
    /// <summary>
    ///     This is added as a component to mind entities, not to player entities.
    ///     <see cref="MindContainerComponent"/> for the one that is added to players.
    ///     A mind represents the IC "mind" of a player.
    ///     Roles are attached as components to its owning entity.
    /// </summary>
    /// <remarks>
    ///     Think of it like this: if a player is supposed to have their memories,
    ///     their mind follows along.
    ///
    ///     Things such as respawning do not follow, because you're a new character.
    ///     Getting borged, cloned, turned into a catbeast, etc... will keep it following you.
    /// </remarks>
    [RegisterComponent]
    public sealed partial class MindComponent : Component
    {
        [DataField]
        public List<EntityUid> Objectives = new();

        /// <summary>
        ///     The session ID of the player owning this mind.
        /// </summary>
        [DataField, Access(typeof(SharedMindSystem))]
        public NetUserId? UserId;

        /// <summary>
        ///     The session ID of the original owner, if any.
        ///     May end up used for round-end information (as the owner may have abandoned Mind since)
        /// </summary>
        [DataField, Access(typeof(SharedMindSystem))]
        public NetUserId? OriginalOwnerUserId;

        /// <summary>
        ///     Entity UID for the first entity that this mind controlled. Used for round end.
        ///     Might be relevant if the player has ghosted since.
        /// </summary>
        [DataField]
        public EntityUid? OriginalOwnedEntity;

        [ViewVariables]
        public bool IsVisitingEntity => VisitingEntity != null;

        [DataField, Access(typeof(SharedMindSystem))]
        public EntityUid? VisitingEntity;

        [ViewVariables]
        public EntityUid? CurrentEntity => VisitingEntity ?? OwnedEntity;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public string? CharacterName;

        /// <summary>
        ///     The time of death for this Mind.
        ///     Can be null - will be null if the Mind is not considered "dead".
        /// </summary>
        [DataField]
        public TimeSpan? TimeOfDeath { get; set; }

        /// <summary>
        ///     The container component currently owned by this mind.
        ///     Can be null.
        /// </summary>
        [ViewVariables]
        public MindContainerComponent? OwnedComponent { get; internal set; }

        /// <summary>
        ///     The entity currently owned by this mind.
        ///     Can be null.
        /// </summary>
        [ViewVariables, Access(typeof(SharedMindSystem))]
        public EntityUid? OwnedEntity;

        // TODO move objectives out of mind component
        /// <summary>
        ///     An enumerable over all the objective entities this mind has.
        /// </summary>
        public IEnumerable<EntityUid> AllObjectives => Objectives;

        /// <summary>
        ///     Prevents user from ghosting out
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool PreventGhosting;

        /// <summary>
        ///     Prevents user from suiciding
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool PreventSuicide;

        /// <summary>
        ///     The session of the player owning this mind.
        ///     Can be null, in which case the player is currently not logged in.
        /// </summary>
        [ViewVariables, Access(typeof(SharedMindSystem), typeof(SharedGameTicker))]
        public ICommonSession? Session { get; set; }
    }
}
