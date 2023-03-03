using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Database;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
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
        private readonly MobStateSystem _mobStateSystem = default!;
        private readonly GameTicker _gameTickerSystem = default!;
        private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        private readonly ISet<Role> _roles = new HashSet<Role>();

        private readonly List<Objective> _objectives = new();

        public string Briefing = String.Empty;

        /// <summary>
        ///     Creates the new mind.
        ///     Note: the Mind is NOT initially attached!
        ///     The provided UserId is solely for tracking of intended owner.
        /// </summary>
        /// <param name="userId">The session ID of the original owner (may get credited).</param>
        public Mind(NetUserId? userId)
        {
            OriginalOwnerUserId = userId;
            IoCManager.InjectDependencies(this);
            _entityManager.EntitySysManager.Resolve(ref _mobStateSystem);
            _entityManager.EntitySysManager.Resolve(ref _gameTickerSystem);
            _entityManager.EntitySysManager.Resolve(ref _mindSystem);
        }

        // TODO: This session should be able to be changed, probably.
        /// <summary>
        ///     The session ID of the player owning this mind.
        /// </summary>
        [ViewVariables]
        public NetUserId? UserId { get; internal set; }

        /// <summary>
        ///     The session ID of the original owner, if any.
        ///     May end up used for round-end information (as the owner may have abandoned Mind since)
        /// </summary>
        [ViewVariables]
        public NetUserId? OriginalOwnerUserId { get; }

        [ViewVariables]
        public bool IsVisitingEntity => VisitingEntity != null;

        [ViewVariables]
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
        public MindComponent? OwnedComponent { get; internal set; }

        /// <summary>
        ///     The entity currently owned by this mind.
        ///     Can be null.
        /// </summary>
        [ViewVariables]
        public EntityUid? OwnedEntity => OwnedComponent?.Owner;

        /// <summary>
        ///     An enumerable over all the roles this mind has.
        /// </summary>
        [ViewVariables]
        public IEnumerable<Role> AllRoles => _roles;

        /// <summary>
        ///     An enumerable over all the objectives this mind has.
        /// </summary>
        [ViewVariables]
        public IEnumerable<Objective> AllObjectives => _objectives;

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
        [ViewVariables]
        public IPlayerSession? Session
        {
            get
            {
                if (!UserId.HasValue)
                {
                    return null;
                }
                _playerManager.TryGetSessionById(UserId.Value, out var ret);
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
        public bool CharacterDeadIC => _mindSystem.IsCharacterDeadPhysically(this);

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
                var targetMobState = _entityManager.GetComponentOrNull<MobStateComponent>(OwnedEntity);
                // This can be null if it's a brain (this happens very often)
                // Brains are the result of gibbing so should definitely count as dead
                if (targetMobState == null)
                    return true;
                // They might actually be alive.
                return _mobStateSystem.IsDead(OwnedEntity!.Value, targetMobState);
            }
        }

        /// <summary>
        ///     A string to represent the mind for logging
        /// </summary>
        private string MindOwnerLoggingString
        {
            get
            {
                if (OwnedEntity != null)
                    return _entityManager.ToPrettyString(OwnedEntity.Value);
                if (UserId != null)
                    return UserId.Value.ToString();
                return "(originally " + OriginalOwnerUserId + ")";
            }
        }

        /// <summary>
        ///     Gives this mind a new role.
        /// </summary>
        /// <param name="role">The type of the role to give.</param>
        /// <returns>The instance of the role.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if we already have a role with this type.
        /// </exception>
        public Role AddRole(Role role)
        {
            if (_roles.Contains(role))
            {
                throw new ArgumentException($"We already have this role: {role}");
            }

            _roles.Add(role);
            role.Greet();

            var message = new RoleAddedEvent(this, role);
            if (OwnedEntity != null)
            {
                _entityManager.EventBus.RaiseLocalEvent(OwnedEntity.Value, message, true);
            }
            _adminLogger.Add(LogType.Mind, LogImpact.Low,
                $"'{role.Name}' added to mind of {MindOwnerLoggingString}");

            return role;
        }

        /// <summary>
        ///     Removes a role from this mind.
        /// </summary>
        /// <param name="role">The type of the role to remove.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown if we do not have this role.
        /// </exception>
        public void RemoveRole(Role role)
        {
            if (!_roles.Contains(role))
            {
                throw new ArgumentException($"We do not have this role: {role}");
            }

            _roles.Remove(role);

            var message = new RoleRemovedEvent(this, role);

            if (OwnedEntity != null)
            {
                _entityManager.EventBus.RaiseLocalEvent(OwnedEntity.Value, message, true);
            }
            _adminLogger.Add(LogType.Mind, LogImpact.Low,
                $"'{role.Name}' removed from mind of {MindOwnerLoggingString}");
        }

        public bool HasRole<T>() where T : Role
        {
            var t = typeof(T);

            return _roles.Any(role => role.GetType() == t);
        }

        /// <summary>
        ///     Gets the current job
        /// </summary>
        public Job? CurrentJob => _roles.OfType<Job>().SingleOrDefault();

        /// <summary>
        /// Adds an objective to this mind.
        /// </summary>
        public bool TryAddObjective(ObjectivePrototype objectivePrototype)
        {
            if (!objectivePrototype.CanBeAssigned(this))
                return false;
            var objective = objectivePrototype.GetObjective(this);
            if (_objectives.Contains(objective))
                return false;

            foreach (var condition in objective.Conditions)
                _adminLogger.Add(LogType.Mind, LogImpact.Low, $"'{condition.Title}' added to mind of {MindOwnerLoggingString}");


            _objectives.Add(objective);
            return true;
        }

        /// <summary>
        /// Removes an objective to this mind.
        /// </summary>
        /// <returns>Returns true if the removal succeeded.</returns>
        public bool TryRemoveObjective(int index)
        {
            if (_objectives.Count >= index) return false;

            var objective = _objectives[index];

            foreach (var condition in objective.Conditions)
                _adminLogger.Add(LogType.Mind, LogImpact.Low, $"'{condition.Title}' removed from the mind of {MindOwnerLoggingString}");

            _objectives.Remove(objective);
            return true;
        }


        public bool TryGetSession([NotNullWhen(true)] out IPlayerSession? session)
        {
            return (session = Session) != null;
        }
    }
}
