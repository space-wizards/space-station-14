using System.Diagnostics;
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Objectives;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Shared.Antag;
using Content.Shared.Antag.Components;
using Content.Shared.Chat;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class ServerAntagSelectionSystem : AntagSelectionSystem
{
    [Dependency] private IBanManager _ban = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IServerPreferencesManager _pref = default!;
    [Dependency] private GhostRoleSystem _ghostRole = default!;
    [Dependency] private PlayTimeTrackingSystem _playTime = default!;

    [SubscribeLocalEvent]
    private void OnTakeGhostRole(Entity<GhostRoleAntagSpawnerComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.TookRole)
            return;

        if (ent.Comp.Rule is not { } rule || ent.Comp.Definition is not { } proto)
            return;

        if (!ProtoMan.Resolve(proto, out var def))
            return;

        if (!Exists(rule) || !RuleQuery.TryComp(rule, out var select))
            return;

        // Ensure the player is allowed to play this antagonist!
        if (IsAntagBanned(args.Player, def) || !_playTime.IsAllowed(args.Player, def.PrefRoles))
            return;

        if (!TrySpawnAntagonist((rule, select), def, args.Player, XForm.GetMapCoordinates(ent), out var uid))
        {
            Log.Error($"Tried to make {args.Player.UserId} into an antagonist but was unable to spawn an entity for them. Game rule {ToPrettyString(ent)}");
            return;
        }

        // We do this after TrySpawnAntagonist so we don't have to worry about a failed spawn adding permanent pre selections to a game rule.
        PreSelectSession((rule, select), def, args.Player);
        InitializeAntag((rule, select), def, uid.Value, args.Player);
        args.TookRole = true;

        // Move ghosts that were watching the raffle on the spawner over to the freshly spawned antag.
        Follower.TransferFollowers(ent.Owner, uid.Value);

        _ghostRole.UnregisterGhostRole((ent, Comp<GhostRoleComponent>(ent)));
    }

    [SubscribeLocalEvent]
    private void OnObjectivesTextGetInfo(Entity<AntagSelectionComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        if (ent.Comp.AgentName is not { } name)
            return;

        args.Minds = GetAntagIdentities(ent.AsNullable()).ToList();
        args.AgentName = Loc.GetString(name);
    }

    [SubscribeLocalEvent]
    private void OnJobNotAssigned(NoJobsAvailableSpawningEvent args)
    {
        // If someone fails to spawn in due to there being no jobs, they should be removed from any preselected antags.
        // We only care about delayed rules, since if they're active the player should have already been removed via MakeAntag.
        var query = QueryDelayedRules();
        while (query.MoveNext(out var uid, out var comp, out _, out _))
        {
            if (comp.SelectionTime == AntagSelectionTime.RuleStarted)
                continue;

            Debug.Assert(comp.SelectionTime != AntagSelectionTime.Never, $"Player: {args.Player.Name}, was pre selected for an game rule {ToPrettyString(uid)} which does not do pre-selections");

            if (!comp.RemoveUponFailedSpawn)
                continue;

            foreach (var antag in comp.Antags)
            {
                if (!comp.PreSelectedSessions.TryGetValue(antag, out var session))
                    break;
                session.Remove(args.Player);
            }
        }
    }

    public override IEnumerable<ProtoId<AntagPrototype>> GetValidAntagPreferences(ICommonSession session, List<ProtoId<AntagPrototype>>? filter = null)
    {
        if (!_pref.TryGetCachedPreferences(session.UserId, out var prefs))
            yield break;

        foreach (var antag in prefs.SelectedCharacter.AntagPreferences)
        {
            // We also check this in IsSessionValid, but we also check it here since this is public API.
            if (_ban.IsRoleBanned(session, antag) || !_playTime.IsAllowed(session, antag))
                continue;

            if (filter != null && !filter.Contains(antag))
                continue;

            yield return antag;
        }
    }

    public override void SendBriefing(ICommonSession? session, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        if (session == null)
            return;

        Audio.PlayGlobal(briefingSound, session);
        if (!string.IsNullOrEmpty(briefing))
        {
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", briefing));
            _chat.ChatMessageToOne(ChatChannel.Server, briefing, wrappedMessage, default, false, session.Channel, briefingColor);
        }
    }

    public override bool IsAntagBanned(ICommonSession session, AntagSpecifierPrototype definition)
    {
        if (_ban.GetAntagBans(session.UserId) is not { } bans)
            return false;

        foreach (var role in definition.PrefRoles)
        {
            // banned!
            if (bans.Contains(role))
                return true;
        }

        return false;
    }

    protected override Entity<AntagSelectionComponent>? ForceGetGameRuleEnt<T>(string id)
    {
        var query = EntityQueryEnumerator<T, AntagSelectionComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            if (MetaData(uid).EntityPrototype?.ID == id)
                return (uid, comp);
        }

        // Game Rule is invalid. You *really* fucked up.
        if (GameTicker.AddGameRule(id) is not { } ruleEnt)
            return null;

        RemComp<LoadMapRuleComponent>(ruleEnt);
        var antag = RuleQuery.Comp(ruleEnt);
        antag.AssignmentHandled = true; // don't do normal selection.
        GameTicker.StartGameRule(ruleEnt.AsNullable());
        return (ruleEnt, antag);
    }
}
