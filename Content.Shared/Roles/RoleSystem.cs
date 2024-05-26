using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public sealed class RoleSystem : EntitySystem
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

        SubscribeAntagEvents<DragonRoleComponent>();
        SubscribeAntagEvents<InitialInfectedRoleComponent>();
        SubscribeAntagEvents<NinjaRoleComponent>();
        SubscribeAntagEvents<NukeopsRoleComponent>();
        SubscribeAntagEvents<RevolutionaryRoleComponent>();
        SubscribeAntagEvents<SubvertedSiliconRoleComponent>();
        SubscribeAntagEvents<TraitorRoleComponent>();
        SubscribeAntagEvents<ZombieRoleComponent>();
        SubscribeAntagEvents<ThiefRoleComponent>();
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

    private void SubscribeAntagEvents<T>() where T : Component, IAntagonistRoleComponent
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

        SubscribeLocalEvent((EntityUid _, T _, ref MindIsAntagonistEvent args) =>
            {
                args.IsAntagonist = true;
                args.IsExclusiveAntagonist |= typeof(T).TryGetCustomAttribute<ExclusiveAntagonistAttribute>(out _);
            });

        SubscribeLocalEvent((EntityUid _, T comp, ref GetBriefingEvent args) =>
            {
                if (comp.Briefing == null)
                    return;

                // loc if exists --
                // it might be filled with pre-localized text (from an entitysystem)
                // or a loc id, from yaml
                var text = comp.Briefing;
                if (Loc.HasString(comp.Briefing))
                    text = Loc.GetString(comp.Briefing);
                args.Append(text, true);
            });

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
            $"Role components {string.Join(components.Keys.ToString(), ", ")} added to mind of {_minds.MindOwnerLoggingString(mind)}");
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
            $"'Role {component}' added to mind of {_minds.MindOwnerLoggingString(mind)}");
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

    /// <summary>
    ///     Formats a complete briefing message from all mind components.
    /// </summary>
    public FormattedMessage? MindGetBriefing(EntityUid? mindId)
    {
        if (mindId == null)
            return null;

        var ev = new GetBriefingEvent();
        RaiseLocalEvent(mindId.Value, ref ev);
        var beforeAntag = ev.Briefings.Aggregate(new FormattedMessage(),
            (messages, next) =>
            {
                messages.AddMessage(next);
                messages.PushNewline();
                messages.PushNewline();
                return messages;
            });

        var afterAntag = ev.AntagBriefings.Aggregate(new FormattedMessage(),
            (messages, next) =>
            {
                messages.AddMessage(next);
                messages.PushNewline();
                messages.PushNewline();
                return messages;
            });

        var finalMsg = new FormattedMessage();
        finalMsg.AddMessage(beforeAntag);
        if (ev.AntagBriefings.Count == 0)
            return finalMsg;

        finalMsg.PushMarkup(Loc.GetString("character-info-middle-antag-greeting"));
        finalMsg.PushNewline();
        finalMsg.AddMessage(afterAntag);

        return finalMsg;
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
}

/// <summary>
/// Event raised on the mind to get its briefing.
/// Handlers should append to the list of messages.
/// </summary>
[ByRefEvent]
public sealed class GetBriefingEvent
{
    public readonly List<FormattedMessage> Briefings = new();
    public readonly List<FormattedMessage> AntagBriefings = new();

    /// <summary>
    /// Adds a new briefing to the event.
    /// If antagonist, sends it to a different list for sorting purposes (we display antag briefings after
    /// all regular briefings)
    /// </summary>
    public void Append(string? text, bool antagonist)
    {
        if (text == null)
            return;

        var msg = FormattedMessage.FromMarkup(text);
        if (antagonist)
            AntagBriefings.Add(msg);
        else
            Briefings.Add(msg);
    }
}
