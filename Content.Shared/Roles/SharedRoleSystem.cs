using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

public abstract class SharedRoleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;

    // TODO please lord make role entities
    private readonly HashSet<Type> _antagTypes = new();

    public override void Initialize()
    {
        // TODO make roles entities
        SubscribeLocalEvent<JobComponent, MindGetAllRolesEvent>(OnJobGetAllRoles);
    }

    private void OnJobGetAllRoles(EntityUid uid, JobComponent component, ref MindGetAllRolesEvent args)
    {
        var name = "game-ticker-unknown-role";
        string? playTimeTracker = null;
        if (component.Prototype != null && _prototypes.TryIndex(component.Prototype, out JobPrototype? job))
        {
            name = job.Name;
            playTimeTracker = job.PlayTimeTracker;
        }

        name = Loc.GetString(name);

        args.Roles.Add(new RoleInfo(component, name, false, playTimeTracker));
    }

    protected void SubscribeAntagEvents<T>() where T : AntagonistRoleComponent
    {
        SubscribeLocalEvent((EntityUid _, T component, ref MindGetAllRolesEvent args) =>
        {
            var name = "game-ticker-unknown-role";
            if (component.PrototypeId != null && _prototypes.TryIndex(component.PrototypeId, out AntagPrototype? antag))
            {
                name = antag.Name;
            }
            name = Loc.GetString(name);

            args.Roles.Add(new RoleInfo(component, name, true, null));
        });

        SubscribeLocalEvent((EntityUid _, T _, ref MindIsAntagonistEvent args) => args.IsAntagonist = true);
        _antagTypes.Add(typeof(T));
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

        var mindEv = new MindRoleAddedEvent();
        RaiseLocalEvent(mindId, ref mindEv);

        var message = new RoleAddedEvent(mindId, mind, antagonist, silent);
        if (mind.OwnedEntity != null)
        {
            RaiseLocalEvent(mind.OwnedEntity.Value, message, true);
        }

        _adminLogger.Add(LogType.Mind, LogImpact.Low,
            $"'Role {typeof(T).Name}' added to mind of {_minds.MindOwnerLoggingString(mind)}");
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
            $"'Role {typeof(T).Name}' removed from mind of {_minds.MindOwnerLoggingString(mind)}");
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
        return HasComp<T>(mindId);
    }

    public List<RoleInfo> MindGetAllRoles(EntityUid mindId)
    {
        var ev = new MindGetAllRolesEvent(new List<RoleInfo>());
        RaiseLocalEvent(mindId, ref ev);
        return ev.Roles;
    }

    public bool MindIsAntagonist(EntityUid? mindId)
    {
        if (mindId == null)
            return false;

        var ev = new MindIsAntagonistEvent();
        RaiseLocalEvent(mindId.Value, ref ev);
        return ev.IsAntagonist;
    }

    public bool IsAntagonistRole<T>()
    {
        return _antagTypes.Contains(typeof(T));
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
}
