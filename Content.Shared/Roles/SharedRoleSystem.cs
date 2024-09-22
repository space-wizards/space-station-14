using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Ghost.Roles;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public abstract class SharedRoleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    // TODO please lord make role entities
    private readonly HashSet<Type> _antagTypes = new();

    private JobRequirementOverridePrototype? _requirementOverride;

    public override void Initialize()
    {
        // TODO make roles entities
        SubscribeLocalEvent<JobComponent, MindGetAllRolesEvent>(OnJobGetAllRoles);
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

    private void OnJobGetAllRoles(EntityUid uid, JobComponent component, ref MindGetAllRolesEvent args)
    {
        var name = "game-ticker-unknown-role";
        var prototype = "";
        string? playTimeTracker = null;
        if (component.Prototype != null && _prototypes.TryIndex(component.Prototype, out JobPrototype? job))
        {
            name = job.Name;
            prototype = job.ID;
            playTimeTracker = job.PlayTimeTracker;
        }

        name = Loc.GetString(name);

        args.Roles.Add(new RoleInfo(component, name, false, playTimeTracker, prototype));
    }

    protected void SubscribeAntagEvents<T>() where T : AntagonistRoleComponent
    {
        SubscribeLocalEvent((EntityUid _, T component, ref MindGetAllRolesEvent args) =>
        {
            var name = "game-ticker-unknown-role";
            var prototype = "";
            if (component.PrototypeId != null && _prototypes.TryIndex(component.PrototypeId, out AntagPrototype? antag))
            {
                name = antag.Name;
                prototype = antag.ID;
            }
            name = Loc.GetString(name);

            args.Roles.Add(new RoleInfo(component, name, true, null, prototype));
        });

        SubscribeLocalEvent((EntityUid _, T _, ref MindIsAntagonistEvent args) => { args.IsAntagonist = true; args.IsExclusiveAntagonist |= typeof(T).TryGetCustomAttribute<ExclusiveAntagonistAttribute>(out _); });
        _antagTypes.Add(typeof(T));
    }

    public void MindAddRoles(EntityUid mindId, ComponentRegistry components, MindComponent? mind = null, bool silent = false)
    {
        if (!Resolve(mindId, ref mind))
            return;

        EntityManager.AddComponents(mindId, components);
        var antagonist = false;
        foreach (var compReg in components.Values)
        {
            var compType = compReg.Component.GetType();

            var comp = EntityManager.ComponentFactory.GetComponent(compType);
            if (IsAntagonistRole(comp.GetType()))
            {
                antagonist = true;
                break;
            }
        }

        var mindEv = new MindRoleAddedEvent(silent);
        RaiseLocalEvent(mindId, ref mindEv);

        var message = new RoleAddedEvent(mindId, mind, antagonist, silent);
        if (mind.OwnedEntity != null)
        {
            RaiseLocalEvent(mind.OwnedEntity.Value, message, true);
        }

        _adminLogger.Add(LogType.Mind, LogImpact.Low,
            $"Role components {string.Join(components.Keys.ToString(), ", ")} added to mind of {mind.OwnedEntity} ({(mind.UserId == null ? "originally " : "")} {mind.UserId ?? mind.OriginalOwnerUserId})");
    }

    public void MindAddRole(EntityUid mindId, Component component, MindComponent? mind = null, bool silent = false)
    {
        if (!Resolve(mindId, ref mind))
            return;

        if (HasComp(mindId, component.GetType()))
        {
            throw new ArgumentException($"We already have this role: {component}");
        }

        EntityManager.AddComponent(mindId, component);
        var antagonist = IsAntagonistRole(component.GetType());

        var mindEv = new MindRoleAddedEvent(silent);
        RaiseLocalEvent(mindId, ref mindEv);

        var message = new RoleAddedEvent(mindId, mind, antagonist, silent);
        if (mind.OwnedEntity != null)
        {
            RaiseLocalEvent(mind.OwnedEntity.Value, message, true);
        }

        _adminLogger.Add(LogType.Mind, LogImpact.Low,
            $"'Role {component}' added to mind of {mind.OwnedEntity} ({(mind.UserId == null ? "originally " : "")} {mind.UserId ?? mind.OriginalOwnerUserId})");
    }

    /// <summary>
    ///     Gives this mind a new role.
    /// </summary>
    /// <param name="mindId">The mind to add the role to.</param>
    /// <param name="component">The role instance to add.</param>
    /// <typeparam name="T">The role type to add.</typeparam>
    /// <param name="silent">Whether or not the role should be added silently</param>
    /// <returns>The instance of the role.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if we already have a role with this type.
    /// </exception>
    public void MindAddRole<T>(EntityUid mindId, T component, MindComponent? mind = null, bool silent = false) where T : IComponent, new()
    {
        if (!Resolve(mindId, ref mind))
            return;

        if (HasComp<T>(mindId))
        {
            throw new ArgumentException($"We already have this role: {typeof(T)}");
        }

        AddComp(mindId, component);
        var antagonist = IsAntagonistRole<T>();

        var mindEv = new MindRoleAddedEvent(silent);
        RaiseLocalEvent(mindId, ref mindEv);

        var message = new RoleAddedEvent(mindId, mind, antagonist, silent);
        if (mind.OwnedEntity != null)
        {
            RaiseLocalEvent(mind.OwnedEntity.Value, message, true);
        }

        _adminLogger.Add(LogType.Mind, LogImpact.Low,
            $"'Role {typeof(T).Name}' added to mind of {mind.OwnedEntity} ({(mind.UserId == null ? "originally " : "")} {mind.UserId ?? mind.OriginalOwnerUserId})");
    }

    /// <summary>
    ///     Removes a role from this mind.
    /// </summary>
    /// <param name="mindId">The mind to remove the role from.</param>
    /// <typeparam name="T">The type of the role to remove.</typeparam>
    /// <exception cref="ArgumentException">
    ///     Thrown if we do not have this role.
    /// </exception>
    public void MindRemoveRole<T>(EntityUid mindId) where T : IComponent
    {
        if (!RemComp<T>(mindId))
        {
            throw new ArgumentException($"We do not have this role: {typeof(T)}");
        }

        var mind = Comp<MindComponent>(mindId);
        var antagonist = IsAntagonistRole<T>();
        var message = new RoleRemovedEvent(mindId, mind, antagonist);

        if (mind.OwnedEntity != null)
        {
            RaiseLocalEvent(mind.OwnedEntity.Value, message, true);
        }
        _adminLogger.Add(LogType.Mind, LogImpact.Low,
            $"'Role {typeof(T).Name}' removed from mind of {mind.OwnedEntity} ({(mind.UserId == null ? "originally " : "")} {mind.UserId ?? mind.OriginalOwnerUserId})");
    }

    public bool MindTryRemoveRole<T>(EntityUid mindId) where T : IComponent
    {
        if (!MindHasRole<T>(mindId))
            return false;

        MindRemoveRole<T>(mindId);
        return true;
    }

    public bool MindHasRole<T>(EntityUid mindId) where T : IComponent
    {
        DebugTools.Assert(HasComp<MindComponent>(mindId));
        return HasComp<T>(mindId);
    }

    public List<RoleInfo> MindGetAllRoles(EntityUid mindId)
    {
        DebugTools.Assert(HasComp<MindComponent>(mindId));
        var ev = new MindGetAllRolesEvent(new List<RoleInfo>());
        RaiseLocalEvent(mindId, ref ev);
        return ev.Roles;
    }

    public bool MindIsAntagonist(EntityUid? mindId)
    {
        if (mindId == null)
            return false;

        DebugTools.Assert(HasComp<MindComponent>(mindId));
        var ev = new MindIsAntagonistEvent();
        RaiseLocalEvent(mindId.Value, ref ev);
        return ev.IsAntagonist;
    }

    /// <summary>
    /// Does this mind possess an exclusive antagonist role
    /// </summary>
    /// <param name="mindId">The mind entity</param>
    /// <returns>True if the mind possesses an exclusive antag role</returns>
    public bool MindIsExclusiveAntagonist(EntityUid? mindId)
    {
        if (mindId == null)
            return false;

        var ev = new MindIsAntagonistEvent();
        RaiseLocalEvent(mindId.Value, ref ev);
        return ev.IsExclusiveAntagonist;
    }

    public bool IsAntagonistRole<T>()
    {
        return _antagTypes.Contains(typeof(T));
    }

    public bool IsAntagonistRole(Type component)
    {
        return _antagTypes.Contains(component);
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

    public HashSet<JobRequirement>? GetJobRequirement(JobPrototype job)
    {
        if (_requirementOverride != null && _requirementOverride.Jobs.TryGetValue(job.ID, out var req))
            return req;

        return job.Requirements;
    }

    public HashSet<JobRequirement>? GetJobRequirement(ProtoId<JobPrototype> job)
    {
        if (_requirementOverride != null && _requirementOverride.Jobs.TryGetValue(job, out var req))
            return req;

        return _prototypes.Index(job).Requirements;
    }

    public HashSet<JobRequirement>? GetAntagRequirement(ProtoId<AntagPrototype> antag)
    {
        if (_requirementOverride != null && _requirementOverride.Antags.TryGetValue(antag, out var req))
            return req;

        return _prototypes.Index(antag).Requirements;
    }

    public HashSet<JobRequirement>? GetAntagRequirement(AntagPrototype antag)
    {
        if (_requirementOverride != null && _requirementOverride.Antags.TryGetValue(antag.ID, out var req))
            return req;

        return antag.Requirements;
    }
}
