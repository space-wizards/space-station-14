using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public abstract class SharedRoleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private JobRequirementOverridePrototype? _requirementOverride;

    public override void Initialize()
    {
        Subs.CVar(_cfg, CCVars.GameRoleTimerOverride, SetRequirementOverride, true);
    }

    private void SetRequirementOverride(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _requirementOverride = null;
            return;
        }

        if (!_prototypes.TryIndex(value, out _requirementOverride ))
            Log.Error($"Unknown JobRequirementOverridePrototype: {value}");
    }

    /// <summary>
    ///     Adds multiple mind roles to a mind
    /// </summary>
    /// <param name="mindId">The mind entity to add the role to</param>
    /// <param name="roles">The list of mind roles to add</param>
    /// <param name="mind">If the mind component is provided, it will be checked if it belongs to the mind entity</param>
    /// <param name="silent">If true, no briefing will be generated upon receiving the mind role</param>
    public void MindAddRoles(EntityUid mindId,
        List<ProtoId<EntityPrototype>>? roles,
        MindComponent? mind = null,
        bool silent = false)
    {
        if (roles is null || roles.Count == 0)
            return;

        foreach (var proto in roles)
        {
            MindAddRole(mindId, proto, mind, silent);
        }
    }

    /// <summary>
    ///     Adds a mind role to a mind
    /// </summary>
    /// <param name="mindId">The mind entity to add the role to</param>
    /// <param name="protoId">The mind role to add</param>
    /// <param name="mind">If the mind component is provided, it will be checked if it belongs to the mind entity</param>
    /// <param name="silent">If true, no briefing will be generated upon receiving the mind role</param>
    public void MindAddRole(EntityUid mindId,
        ProtoId<EntityPrototype> protoId,
        MindComponent? mind = null,
        bool silent = false)
    {
        if (protoId == "MindRoleJob")
            MindAddJobRole(mindId, mind, silent, "");
        else
            MindAddRoleDo(mindId, protoId, mind, silent);
    }

    /// <summary>
    /// Adds a Job mind role with the specified job prototype
    /// </summary>
    /// /// <param name="mindId">The mind entity to add the job role to</param>
    /// <param name="mind">If the mind component is provided, it will be checked if it belongs to the mind entity</param>
    /// <param name="silent">If true, no briefing will be generated upon receiving the mind role</param>
    /// <param name="jobPrototype">The Job prototype for the new role</param>
    public void MindAddJobRole(EntityUid mindId,
        MindComponent? mind = null,
        bool silent = false,
        string? jobPrototype = null)
    {
        if (!Resolve(mindId, ref mind))
            return;

        // Can't have someone get paid for two jobs now, can we
        if (MindHasRole<JobRoleComponent>((mindId, mind), out var jobRole)
            && jobRole.Value.Comp1.JobPrototype != jobPrototype)
        {
            _adminLogger.Add(LogType.Mind,
                LogImpact.Low,
                $"Job Role of {ToPrettyString(mind.OwnedEntity)} changed from '{jobRole.Value.Comp1.JobPrototype}' to '{jobPrototype}'");

            jobRole.Value.Comp1.JobPrototype = jobPrototype;
        }
        else
            MindAddRoleDo(mindId, "MindRoleJob", mind, silent, jobPrototype);
    }

    /// <summary>
    ///     Creates a Mind Role
    /// </summary>
    private void MindAddRoleDo(EntityUid mindId,
        ProtoId<EntityPrototype> protoId,
        MindComponent? mind = null,
        bool silent = false,
        string? jobPrototype = null)
    {
        if (!Resolve(mindId, ref mind))
        {
            Log.Error($"Failed to add role {protoId} to mind {mindId} : Mind does not match provided mind component");
            return;
        }

        var antagonist = false;

        if (!_prototypes.TryIndex(protoId, out var protoEnt))
        {
            Log.Error($"Failed to add role {protoId} to mind {mindId} : Role prototype does not exist");
            return;
        }

        //TODO don't let a prototype being added a second time
        //If that was somehow to occur, a second mindrole for that comp would be created
        //Meaning any mind role checks could return wrong results, since they just return the first match they find

        var mindRoleId = Spawn(protoId, MapCoordinates.Nullspace);
        EnsureComp<MindRoleComponent>(mindRoleId);
        var mindRoleComp = Comp<MindRoleComponent>(mindRoleId);

        mindRoleComp.Mind = (mindId,mind);
        if (jobPrototype is not null)
        {
            mindRoleComp.JobPrototype = jobPrototype;
            EnsureComp<JobRoleComponent>(mindRoleId);
            DebugTools.AssertNull(mindRoleComp.AntagPrototype);
            DebugTools.Assert(!mindRoleComp.Antag);
            DebugTools.Assert(!mindRoleComp.ExclusiveAntag);
        }

        antagonist |= mindRoleComp.Antag;
        mind.MindRoles.Add(mindRoleId);

        var mindEv = new MindRoleAddedEvent(silent);
        RaiseLocalEvent(mindId, ref mindEv);

        var message = new RoleAddedEvent(mindId, mind, antagonist, silent);
        if (mind.OwnedEntity != null)
        {
            RaiseLocalEvent(mind.OwnedEntity.Value, message, true);
        }

        var name = Loc.GetString(protoEnt.Name);
        if (mind.OwnedEntity is not null)
        {
            _adminLogger.Add(LogType.Mind,
                LogImpact.Low,
                $"{name} added to mind of {ToPrettyString(mind.OwnedEntity)}");
        }
        else
        {
            //TODO: This is not tied to the player on the Admin Log filters.
            //Probably only happens when Job Role is added on initial spawn, before the mind entity is put in a mob
            _adminLogger.Add(LogType.Mind,
                LogImpact.Low,
                $"{name} added to {ToPrettyString(mindId)}");
        }
    }

    /// <summary>
    ///     Removes all instances of a specific role from this mind.
    /// </summary>
    /// <param name="mind">The mind to remove the role from.</param>
    /// <typeparam name="T">The type of the role to remove.</typeparam>
    /// <returns>Returns false if the role did not exist. True if successful</returns>>
    public bool MindRemoveRole<T>(Entity<MindComponent?> mind) where T : IComponent
    {
        if (typeof(T) == typeof(MindRoleComponent))
            throw new InvalidOperationException();

        if (!Resolve(mind.Owner, ref mind.Comp))
            return false;

        var found = false;
        var antagonist = false;
        var delete = new List<EntityUid>();
        foreach (var role in mind.Comp.MindRoles)
        {
            if (!HasComp<T>(role))
                continue;

            if (!TryComp(role, out MindRoleComponent? roleComp))
            {
                Log.Error($"Encountered mind role entity {ToPrettyString(role)} without a {nameof(MindRoleComponent)}");
                continue;
            }

            antagonist |= roleComp.Antag | roleComp.ExclusiveAntag;
            _entityManager.DeleteEntity(role);
            delete.Add(role);
            found = true;
        }

        if (!found)
            return false;

        foreach (var role in delete)
        {
            mind.Comp.MindRoles.Remove(role);
        }

        if (mind.Comp.OwnedEntity != null)
        {
            var message = new RoleRemovedEvent(mind.Owner, mind.Comp, antagonist);
            RaiseLocalEvent(mind.Comp.OwnedEntity.Value, message, true);
        }

        _adminLogger.Add(LogType.Mind,
            LogImpact.Low,
            $"All roles of type '{typeof(T).Name}' removed from mind of {ToPrettyString(mind.Comp.OwnedEntity)}");

        return true;
    }

    /// <summary>
    /// Finds and removes all mind roles of a specific type
    /// </summary>
    /// <param name="mindId">The mind entity</param>
    /// <typeparam name="T">The type of the role to remove.</typeparam>
    /// <returns>True if the role existed and was removed</returns>
    public bool MindTryRemoveRole<T>(EntityUid mindId) where T : IComponent
    {
        if (typeof(T) == typeof(MindRoleComponent))
            return false;

        if (MindRemoveRole<T>(mindId))
            return true;

        Log.Warning($"Failed to remove role {typeof(T)} from {ToPrettyString(mindId)} : mind does not have role ");
        return false;
    }

    /// <summary>
    /// Finds the first mind role of a specific T type on a mind entity.
    /// Outputs entity components for the mind role's MindRoleComponent and for T
    /// </summary>
    /// <param name="mindId">The mind entity</param>
    /// <typeparam name="T">The type of the role to find.</typeparam>
    /// <param name="role">The Mind Role entity component</param>
    /// <param name="roleT">The Mind Role's entity component for T</param>
    /// <returns>True if the role is found</returns>
    public bool MindHasRole<T>(Entity<MindComponent?> mind,
        [NotNullWhen(true)] out Entity<MindRoleComponent, T>? role) where T : IComponent
    {
        role = null;
        if (!Resolve(mind.Owner, ref mind.Comp))
            return false;

        foreach (var roleEnt in mind.Comp.MindRoles)
        {
            if (!TryComp(roleEnt, out T? tcomp))
                continue;

            if (!TryComp(roleEnt, out MindRoleComponent? roleComp))
            {
                Log.Error($"Encountered mind role entity {ToPrettyString(roleEnt)} without a {nameof(MindRoleComponent)}");
                continue;
            }

            role = (roleEnt, roleComp, tcomp);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the first mind role of a specific type on a mind entity.
    /// Outputs an entity component for the mind role's MindRoleComponent
    /// </summary>
    /// <param name="mindId">The mind entity</param>
    /// <param name="type">The Type to look for</param>
    /// <param name="role">The output role</param>
    /// <returns>True if the role is found</returns>
    public bool MindHasRole(EntityUid mindId,
        Type type,
        [NotNullWhen(true)] out Entity<MindRoleComponent>? role)
    {
        role = null;
        // All MindRoles have this component, it would just return the first one.
        // Order might not be what is expected.
        // Better to report null
        if (type == Type.GetType("MindRoleComponent"))
        {
            Log.Error($"Something attempted to query mind role 'MindRoleComponent' on mind {mindId}. This component is present on every single mind role.");
            return false;
        }

        if (!TryComp<MindComponent>(mindId, out var mind))
            return false;

        var found = false;

        foreach (var roleEnt in mind.MindRoles)
        {
            if (!HasComp(roleEnt, type))
                continue;

            if (!TryComp(roleEnt, out MindRoleComponent? roleComp))
            {
                Log.Error($"Encountered mind role entity {ToPrettyString(roleEnt)} without a {nameof(MindRoleComponent)}");
                continue;
            }

            role = (roleEnt, roleComp);
            found = true;
            break;
        }

        return found;
    }

    /// <summary>
    /// Finds the first mind role of a specific type on a mind entity.
    /// </summary>
    /// <param name="mindId">The mind entity</param>
    /// <typeparam name="T">The type of the role to find.</typeparam>
    /// <returns>True if the role is found</returns>
    public bool MindHasRole<T>(EntityUid mindId) where T : IComponent
    {
        return MindHasRole<T>(mindId, out _);
    }

    //TODO: Delete this later
    /// <summary>
    /// Returns the first mind role of a specific type
    /// </summary>
    /// <param name="mindId">The mind entity</param>
    /// <returns>Entity Component of the mind role</returns>
    [Obsolete("Use MindHasRole's output value")]
    public Entity<MindRoleComponent>? MindGetRole<T>(EntityUid mindId) where T : IComponent
    {
        Entity<MindRoleComponent>? result = null;

        var mind = Comp<MindComponent>(mindId);

        foreach (var uid in mind.MindRoles)
        {
            if (HasComp<T>(uid) && TryComp<MindRoleComponent>(uid, out var comp))
                result = (uid,comp);
        }
        return result;
    }

    /// <summary>
    /// Reads all Roles of a mind Entity and returns their data as RoleInfo
    /// </summary>
    /// <param name="mind">The mind entity</param>
    /// <returns>RoleInfo list</returns>
    public List<RoleInfo> MindGetAllRoleInfo(Entity<MindComponent?> mind)
    {
        var roleInfo = new List<RoleInfo>();

        if (!Resolve(mind.Owner, ref mind.Comp))
            return roleInfo;

        foreach (var role in mind.Comp.MindRoles)
        {
            var valid = false;
            var name = "game-ticker-unknown-role";
            var prototype = "";
            string? playTimeTracker = null;

            if (!TryComp(role, out MindRoleComponent? comp))
            {
                Log.Error($"Encountered mind role entity {ToPrettyString(role)} without a {nameof(MindRoleComponent)}");
                continue;
            }

            if (comp.AntagPrototype is not null)
                prototype = comp.AntagPrototype;

            if (comp.JobPrototype is not null && comp.AntagPrototype is null)
            {
                prototype = comp.JobPrototype;
                if (_prototypes.TryIndex(comp.JobPrototype, out var job))
                {
                    playTimeTracker = job.PlayTimeTracker;
                    name = job.Name;
                    valid = true;
                }
                else
                {
                    Log.Error($" Mind Role Prototype '{role.Id}' contains invalid Job prototype: '{comp.JobPrototype}'");
                }
            }
            else if (comp.AntagPrototype is not null && comp.JobPrototype is null)
            {
                prototype = comp.AntagPrototype;
                if (_prototypes.TryIndex(comp.AntagPrototype, out var antag))
                {
                    name = antag.Name;
                    valid = true;
                }
                else
                {
                    Log.Error($" Mind Role Prototype '{role.Id}' contains invalid Antagonist prototype: '{comp.AntagPrototype}'");
                }
            }
            else if (comp.JobPrototype is not null && comp.AntagPrototype is not null)
            {
                Log.Error($" Mind Role Prototype '{role.Id}' contains both Job and Antagonist prototypes");
            }

            if (valid)
                roleInfo.Add(new RoleInfo(name, comp.Antag, playTimeTracker, prototype));
        }
        return roleInfo;
    }

    /// <summary>
    /// Does this mind possess an antagonist role
    /// </summary>
    /// <param name="mindId">The mind entity</param>
    /// <returns>True if the mind possesses any antag roles</returns>
    public bool MindIsAntagonist(EntityUid? mindId)
    {
        if (mindId is null)
            return false;

        return CheckAntagonistStatus(mindId.Value).Antag;
    }

    /// <summary>
    /// Does this mind possess an exclusive antagonist role
    /// </summary>
    /// <param name="mindId">The mind entity</param>
    /// <returns>True if the mind possesses any exclusive antag roles</returns>
    public bool MindIsExclusiveAntagonist(EntityUid? mindId)
    {
        if (mindId is null)
            return false;

        return CheckAntagonistStatus(mindId.Value).ExclusiveAntag;
    }

   public (bool Antag, bool ExclusiveAntag) CheckAntagonistStatus(Entity<MindComponent?> mind)
   {
       if (!Resolve(mind.Owner, ref mind.Comp))
           return (false, false);

        var antagonist = false;
        var exclusiveAntag = false;
        foreach (var role in mind.Comp.MindRoles)
        {
            if (!TryComp<MindRoleComponent>(role, out var roleComp))
            {
                Log.Error($"Mind Role Entity {ToPrettyString(role)} does not have a MindRoleComponent, despite being listed as a role belonging to {ToPrettyString(mind)}|");
                continue;
            }

            antagonist |= roleComp.Antag;
            exclusiveAntag |= roleComp.ExclusiveAntag;
        }

        return (antagonist, exclusiveAntag);
    }

    /// <summary>
    /// Play a sound for the mind, if it has a session attached.
    /// Use this for role greeting sounds.
    /// </summary>
    public void MindPlaySound(EntityUid mindId, SoundSpecifier? sound, MindComponent? mind = null)
    {
        if (Resolve(mindId, ref mind) && mind.Session != null)
            _audio.PlayGlobal(sound, mind.Session);
    }

    // TODO ROLES Change to readonly.
    // Passing around a reference to a prototype's hashset makes me uncomfortable because it might be accidentally
    // mutated.
    public HashSet<JobRequirement>? GetJobRequirement(JobPrototype job)
    {
        if (_requirementOverride != null && _requirementOverride.Jobs.TryGetValue(job.ID, out var req))
            return req;

        return job.Requirements;
    }

    // TODO ROLES Change to readonly.
    public HashSet<JobRequirement>? GetJobRequirement(ProtoId<JobPrototype> job)
    {
        if (_requirementOverride != null && _requirementOverride.Jobs.TryGetValue(job, out var req))
            return req;

        return _prototypes.Index(job).Requirements;
    }

    // TODO ROLES Change to readonly.
    public HashSet<JobRequirement>? GetAntagRequirement(ProtoId<AntagPrototype> antag)
    {
        if (_requirementOverride != null && _requirementOverride.Antags.TryGetValue(antag, out var req))
            return req;

        return _prototypes.Index(antag).Requirements;
    }

    // TODO ROLES Change to readonly.
    public HashSet<JobRequirement>? GetAntagRequirement(AntagPrototype antag)
    {
        if (_requirementOverride != null && _requirementOverride.Antags.TryGetValue(antag.ID, out var req))
            return req;

        return antag.Requirements;
    }
}
