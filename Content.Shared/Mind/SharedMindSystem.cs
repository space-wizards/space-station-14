using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Systems;
using Content.Shared.Players;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Mind;

public abstract class SharedMindSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedPlayerSystem _player = default!;

    // This is dictionary is required to track the minds of disconnected players that may have had their entity deleted.
    protected readonly Dictionary<NetUserId, EntityUid> UserMinds = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MindContainerComponent, SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<VisitingMindComponent, EntityTerminatingEvent>(OnVisitingTerminating);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnReset);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        WipeAllMinds();
    }

    private void OnReset(RoundRestartCleanupEvent ev)
    {
        WipeAllMinds();
    }

    public virtual void WipeAllMinds()
    {
        foreach (var mind in UserMinds.Values)
        {
            WipeMind(mind);
        }

        DebugTools.Assert(UserMinds.Count == 0);
    }

    public EntityUid? GetMind(NetUserId user)
    {
        TryGetMind(user, out var mind, out _);
        return mind;
    }

    public virtual bool TryGetMind(NetUserId user, [NotNullWhen(true)] out EntityUid? mindId, [NotNullWhen(true)] out MindComponent? mind)
    {
        if (UserMinds.TryGetValue(user, out var mindIdValue) &&
            TryComp(mindIdValue, out mind))
        {
            DebugTools.Assert(mind.UserId == user);
            mindId = mindIdValue;
            return true;
        }

        mindId = null;
        mind = null;
        return false;
    }

    private void OnVisitingTerminating(EntityUid uid, VisitingMindComponent component, ref EntityTerminatingEvent args)
    {
        if (component.MindId != null)
            UnVisit(component.MindId.Value);
    }

    private void OnExamined(EntityUid uid, MindContainerComponent mindContainer, ExaminedEvent args)
    {
        if (!mindContainer.ShowExamineInfo || !args.IsInDetailsRange)
            return;

        var dead = _mobState.IsDead(uid);
        var hasSession = CompOrNull<MindComponent>(mindContainer.Mind)?.Session;

        if (dead && !mindContainer.HasMind)
            args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-dead-and-irrecoverable", ("ent", uid))}[/color]");
        else if (dead && hasSession == null)
            args.PushMarkup($"[color=yellow]{Loc.GetString("comp-mind-examined-dead-and-ssd", ("ent", uid))}[/color]");
        else if (dead)
            args.PushMarkup($"[color=red]{Loc.GetString("comp-mind-examined-dead", ("ent", uid))}[/color]");
        else if (!mindContainer.HasMind)
            args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-catatonic", ("ent", uid))}[/color]");
        else if (hasSession == null)
            args.PushMarkup($"[color=yellow]{Loc.GetString("comp-mind-examined-ssd", ("ent", uid))}[/color]");
    }

    private void OnSuicide(EntityUid uid, MindContainerComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(component.Mind, out MindComponent? mind) && mind.PreventSuicide)
        {
            args.BlockSuicideAttempt(true);
        }
    }

    public EntityUid? GetMind(EntityUid uid, MindContainerComponent? mind = null)
    {
        if (!Resolve(uid, ref mind))
            return null;

        if (mind.HasMind)
            return mind.Mind;

        return null;
    }

    public EntityUid CreateMind(NetUserId? userId, string? name = null)
    {
        var mindId = Spawn(null, MapCoordinates.Nullspace);
        var mind = EnsureComp<MindComponent>(mindId);
        mind.CharacterName = name;
        SetUserId(mindId, userId, mind);

        Dirty(mindId, MetaData(mindId));

        return mindId;
    }

    /// <summary>
    ///     True if the OwnedEntity of this mind is physically dead.
    ///     This specific definition, as opposed to CharacterDeadIC, is used to determine if ghosting should allow return.
    /// </summary>
    public bool IsCharacterDeadPhysically(MindComponent mind)
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

        if (mind.OwnedEntity == null)
            return true;

        // This can be null if they're deleted (spike / brain nom)
        var targetMobState = EntityManager.GetComponentOrNull<MobStateComponent>(mind.OwnedEntity);
        // This can be null if it's a brain (this happens very often)
        // Brains are the result of gibbing so should definitely count as dead
        if (targetMobState == null)
            return true;
        // They might actually be alive.
        return _mobState.IsDead(mind.OwnedEntity.Value, targetMobState);
    }

    public virtual void Visit(EntityUid mindId, EntityUid entity, MindComponent? mind = null)
    {
    }

    /// <summary>
    /// Returns the mind to its original entity.
    /// </summary>
    public virtual void UnVisit(EntityUid mindId, MindComponent? mind = null)
    {
    }

    /// <summary>
    /// Returns the mind to its original entity.
    /// </summary>
    public void UnVisit(ICommonSession? player)
    {
        if (player == null || !TryGetMind(player, out var mindId, out var mind))
            return;

        UnVisit(mindId, mind);
    }

    /// <summary>
    /// Cleans up the VisitingEntity.
    /// </summary>
    /// <param name="mind"></param>
    protected void RemoveVisitingEntity(MindComponent mind)
    {
        if (mind.VisitingEntity == null)
            return;

        var oldVisitingEnt = mind.VisitingEntity.Value;
        // Null this before removing the component to avoid any infinite loops.
        mind.VisitingEntity = null;

        if (TryComp(oldVisitingEnt, out VisitingMindComponent? visitComp))
        {
            visitComp.MindId = null;
            RemCompDeferred(oldVisitingEnt, visitComp);
        }

        RaiseLocalEvent(oldVisitingEnt, new MindUnvisitedMessage(), true);
    }

    public void WipeMind(ICommonSession player)
    {
        var mind = _player.ContentData(player)?.Mind;
        DebugTools.Assert(GetMind(player.UserId) == mind);
        WipeMind(mind);
    }

    /// <summary>
    /// Detaches a mind from all entities and clears the user ID.
    /// </summary>
    public void WipeMind(EntityUid? mindId, MindComponent? mind = null)
    {
        if (mindId == null || !Resolve(mindId.Value, ref mind, false))
            return;

        TransferTo(mindId.Value, null, mind: mind);
        SetUserId(mindId.Value, null, mind: mind);
    }

    /// <summary>
    ///     Transfer this mind's control over to a new entity.
    /// </summary>
    /// <param name="mindId">The mind to transfer</param>
    /// <param name="entity">
    ///     The entity to control.
    ///     Can be null, in which case it will simply detach the mind from any entity.
    /// </param>
    /// <param name="ghostCheckOverride">
    ///     If true, skips ghost check for Visiting Entity
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="entity"/> is already controlled by another player.
    /// </exception>
    public virtual void TransferTo(EntityUid mindId, EntityUid? entity, bool ghostCheckOverride = false, bool createGhost = true, MindComponent? mind = null)
    {
    }

    /// <summary>
    /// Tries to create and add an objective from its prototype id.
    /// </summary>
    /// <returns>Returns true if adding the objective succeeded.</returns>
    public bool TryAddObjective(EntityUid mindId, MindComponent mind, string proto)
    {
        var objective = _objectives.TryCreateObjective(mindId, mind, proto);
        if (objective == null)
            return false;

        AddObjective(mindId, mind, objective.Value);
        return true;
    }

    /// <summary>
    /// Adds an objective that already exists, and is assumed to have had its requirements checked.
    /// </summary>
    public void AddObjective(EntityUid mindId, MindComponent mind, EntityUid objective)
    {
        var title = Name(objective);
        _adminLogger.Add(LogType.Mind, LogImpact.Low, $"Objective {objective} ({title}) added to mind of {MindOwnerLoggingString(mind)}");
        mind.Objectives.Add(objective);
    }

    /// <summary>
    /// Removes an objective from this mind.
    /// </summary>
    /// <returns>Returns true if the removal succeeded.</returns>
    public bool TryRemoveObjective(EntityUid mindId, MindComponent mind, int index)
    {
        if (index < 0 || index >= mind.Objectives.Count)
            return false;

        var objective = mind.Objectives[index];

        var title = Name(objective);
        _adminLogger.Add(LogType.Mind, LogImpact.Low, $"Objective {objective} ({title}) removed from the mind of {MindOwnerLoggingString(mind)}");
        mind.Objectives.Remove(objective);
        Del(objective);
        return true;
    }

    public bool TryGetSession(EntityUid? mindId, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;
        return TryComp(mindId, out MindComponent? mind) && (session = mind.Session) != null;
    }

    /// <summary>
    /// Gets a mind from uid and/or MindContainerComponent. Used for null checks.
    /// </summary>
    /// <param name="uid">Entity UID that owns the mind.</param>
    /// <param name="mindId">The mind id.</param>
    /// <param name="mind">The returned mind.</param>
    /// <param name="container">Mind component on <paramref name="uid"/> to get the mind from.</param>
    /// <returns>True if mind found. False if not.</returns>
    public bool TryGetMind(
        EntityUid uid,
        out EntityUid mindId,
        [NotNullWhen(true)] out MindComponent? mind,
        MindContainerComponent? container = null)
    {
        mindId = default;
        mind = null;

        if (!Resolve(uid, ref container, false))
            return false;

        if (!container.HasMind)
            return false;

        mindId = container.Mind ?? default;
        return TryComp(mindId, out mind);
    }

    public bool TryGetMind(
        PlayerData player,
        out EntityUid mindId,
        [NotNullWhen(true)] out MindComponent? mind)
    {
        mindId = player.Mind ?? default;
        return TryComp(mindId, out mind);
    }

    public bool TryGetMind(
        ICommonSession? player,
        out EntityUid mindId,
        [NotNullWhen(true)] out MindComponent? mind)
    {
        mindId = default;
        mind = null;
        return _player.ContentData(player) is { } data && TryGetMind(data, out mindId, out mind);
    }

    /// <summary>
    /// Gets a role component from a player's mind.
    /// </summary>
    /// <returns>Whether a role was found</returns>
    public bool TryGetRole<T>(EntityUid user, [NotNullWhen(true)] out T? role) where T : Component
    {
        role = null;
        if (!TryComp<MindContainerComponent>(user, out var mindContainer) || mindContainer.Mind == null)
            return false;

        return TryComp(mindContainer.Mind, out role);
    }

    /// <summary>
    /// Sets the Mind's OwnedComponent and OwnedEntity
    /// </summary>
    /// <param name="mind">Mind to set OwnedComponent and OwnedEntity on</param>
    /// <param name="uid">Entity owned by <paramref name="mind"/></param>
    /// <param name="mindContainerComponent">MindContainerComponent owned by <paramref name="mind"/></param>
    protected void SetOwnedEntity(MindComponent mind, EntityUid? uid, MindContainerComponent? mindContainerComponent)
    {
        if (uid != null)
            Resolve(uid.Value, ref mindContainerComponent);

        mind.OwnedEntity = uid;
        mind.OwnedComponent = mindContainerComponent;
    }

    /// <summary>
    /// Sets the Mind's UserId, Session, and updates the player's PlayerData. This should have no direct effect on the
    /// entity that any mind is connected to, except as a side effect of the fact that it may change a player's
    /// attached entity. E.g., ghosts get deleted.
    /// </summary>
    public virtual void SetUserId(EntityUid mindId, NetUserId? userId, MindComponent? mind = null)
    {
    }

    /// <summary>
    ///     True if this Mind is 'sufficiently dead' IC (Objectives, EndText).
    ///     Note that this is *IC logic*, it's not necessarily tied to any specific truth.
    ///     "If administrators decide that zombies are dead, this returns true for zombies."
    ///     (Maybe you were looking for the action blocker system?)
    /// </summary>
    public bool IsCharacterDeadIc(MindComponent mind)
    {
        if (mind.OwnedEntity is { } owned)
        {
            var ev = new GetCharactedDeadIcEvent(null);
            RaiseLocalEvent(owned, ref ev);

            if (ev.Dead != null)
                return ev.Dead.Value;
        }

        return IsCharacterDeadPhysically(mind);
    }

    /// <summary>
    ///     A string to represent the mind for logging
    /// </summary>
    public string MindOwnerLoggingString(MindComponent mind)
    {
        if (mind.OwnedEntity != null)
            return ToPrettyString(mind.OwnedEntity.Value);
        if (mind.UserId != null)
            return mind.UserId.Value.ToString();
        return "(originally " + mind.OriginalOwnerUserId + ")";
    }

    public string? GetCharacterName(NetUserId userId)
    {
        return TryGetMind(userId, out _, out var mind) ? mind.CharacterName : null;
    }

    /// <summary>
    /// Returns a list of every living humanoid player's minds, except for a single one which is exluded.
    /// </summary>
    public List<EntityUid> GetAliveHumansExcept(EntityUid exclude)
    {
        var mindQuery = EntityQuery<MindComponent>();

        var allHumans = new List<EntityUid>();
        // HumanoidAppearanceComponent is used to prevent mice, pAIs, etc from being chosen
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uid, out var mc, out var mobState, out _))
        {
            // the player needs to have a mind and not be the excluded one
            if (mc.Mind == null || mc.Mind == exclude)
                continue;

            // the player has to be alive
            if (_mobState.IsAlive(uid, mobState))
                allHumans.Add(mc.Mind.Value);
        }

        return allHumans;
    }
}

/// <summary>
/// Raised on an entity to determine whether or not they are "dead" in IC-logic.
/// If not handled, then it will simply check if they are dead physically.
/// </summary>
/// <param name="Dead"></param>
[ByRefEvent]
public record struct GetCharactedDeadIcEvent(bool? Dead);
