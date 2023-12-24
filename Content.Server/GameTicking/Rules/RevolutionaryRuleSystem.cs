using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Flash;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared.Chat;
using Content.Shared.Cloning;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.Stunnable;
using Content.Shared.Zombies;
using Robust.Server.Audio;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for the game to end.)
/// </summary>
public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    [ValidatePrototypeId<AntagPrototype>]
    public const string RevolutionaryAntagRole = "Rev";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
        SubscribeLocalEvent<CommandStaffComponent, ChangedGridEvent>(OnCommandGridChanged);
        SubscribeLocalEvent<CommandStaffComponent, MobStateChangedEvent>(OnCommandMobStateChanged);
        SubscribeLocalEvent<HeadRevolutionaryComponent, ChangedGridEvent>(OnHeadRevGridChanged);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MobStateChangedEvent>(OnHeadRevMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<RevolutionaryRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<HeadRevolutionaryComponent, AfterFlashedEvent>(OnPostFlash);
        SubscribeLocalEvent<RevolutionaryComponent, CloningEvent>(OnClone);
    }

    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var revsLost = CheckRevsLose(true);
        var commandLost = CheckCommandLose(true);
        var query = AllEntityQuery<RevolutionaryRuleComponent>();
        while (query.MoveNext(out var headrev))
        {
            // This is (revsLost, commandsLost) concatted together
            // (moony wrote this comment idk what it means)
            var index = (commandLost ? 1 : 0) | (revsLost ? 2 : 0);
            ev.AddLine(Loc.GetString(Outcomes[index]));

            ev.AddLine(Loc.GetString("rev-headrev-count", ("initialCount", headrev.HeadRevs.Count)));
            foreach (var player in headrev.HeadRevs.Values)
            {
                var title = _objectives.GetTitle(player);
                if (title == null)
                    continue;

                // TODO: when role entities are a thing this has to change
                var count = CompOrNull<RevolutionaryRoleComponent>(player)?.ConvertedCount ?? 0;
                ev.AddLine(Loc.GetString("rev-headrev-player", ("title", title), ("count", count)));

                // TODO: someone suggested listing all alive? revs maybe implement at some point
            }
        }
    }

    private void OnGetBriefing(EntityUid uid, RevolutionaryRoleComponent comp, ref GetBriefingEvent args)
    {
        if (!TryComp<MindComponent>(uid, out var mind) || mind.OwnedEntity == null)
            return;

        var head = HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity);
        args.Append(Loc.GetString(head ? "head-rev-briefing" : "rev-briefing"));
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            _antagSelection.AttemptStartGameRule(ev, uid, comp.MinPlayers, gameRule);
        }
    }

    private void OnPlayerJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var comp, out _))
        {
            _antagSelection.EligiblePlayers(comp.HeadRevPrototypeId, comp.MaxHeadRevs, comp.PlayersPerHeadRev, comp.HeadRevStartSound,
                "head-rev-role-greeting", "#5e9cff", out var chosen);
            if (chosen.Any())
                GiveHeadRev(chosen, comp.HeadRevPrototypeId, comp);
            else
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("rev-no-heads"));
            }
        }
    }

    private void GiveHeadRev(List<EntityUid> chosen, string antagProto, RevolutionaryRuleComponent comp)
    {
        foreach (var headRev in chosen)
        {
            RemComp<CommandStaffComponent>(headRev);

            var inCharacterName = MetaData(headRev).EntityName;
            if (_mind.TryGetMind(headRev, out var mindId, out var mind))
            {
                if (!_role.MindHasRole<RevolutionaryRoleComponent>(mindId))
                {
                    _role.MindAddRole(mindId, new RevolutionaryRoleComponent { PrototypeId = antagProto });
                }
                if (mind.Session != null)
                {
                    comp.HeadRevs.Add(inCharacterName, mindId);
                }
            }

            _antagSelection.GiveAntagBagGear(headRev, comp.StartingGear);
            EnsureComp<RevolutionaryComponent>(headRev);
            EnsureComp<HeadRevolutionaryComponent>(headRev);
        }
    }

    /// <summary>
    /// Called when a Head Rev uses a flash in melee to convert somebody else.
    /// </summary>
    public void OnPostFlash(EntityUid uid, HeadRevolutionaryComponent comp, ref AfterFlashedEvent ev)
    {
        TryComp<AlwaysRevolutionaryConvertibleComponent>(ev.Target, out var alwaysConvertibleComp);
        var alwaysConvertible = alwaysConvertibleComp != null;

        if (!_mind.TryGetMind(ev.Target, out var mindId, out var mind) && !alwaysConvertible)
            return;

        if (HasComp<RevolutionaryComponent>(ev.Target) ||
            HasComp<MindShieldComponent>(ev.Target) ||
            !HasComp<HumanoidAppearanceComponent>(ev.Target) &&
            !alwaysConvertible ||
            !_mobState.IsAlive(ev.Target) ||
            HasComp<ZombieComponent>(ev.Target))
        {
            return;
        }

        EnsureComp<RevolutionaryComponent>(ev.Target);
        _stun.TryParalyze(ev.Target, comp.StunTime, true);
        if (ev.User != null)
        {
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Revolutionary");

            if (_mind.TryGetRole<RevolutionaryRoleComponent>(ev.User.Value, out var headrev))
                headrev.ConvertedCount++;
        }

        if (mindId == default || !_role.MindHasRole<RevolutionaryRoleComponent>(mindId))
        {
            _role.MindAddRole(mindId, new RevolutionaryRoleComponent { PrototypeId = RevolutionaryAntagRole });
        }
        if (mind?.Session != null)
        {
            var message = Loc.GetString("rev-role-greeting");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.Red);
            _audioSystem.PlayGlobal("/Audio/Ambience/Antag/headrev_start.ogg", ev.Target);
        }
    }

    public void OnHeadRevAdmin(EntityUid mindId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return;

        var revRule = EntityQuery<RevolutionaryRuleComponent>().FirstOrDefault();
        if (revRule == null)
        {
            GameTicker.StartGameRule("Revolutionary", out var ruleEnt);
            revRule = Comp<RevolutionaryRuleComponent>(ruleEnt);
        }

        if (!HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity))
        {
            if (mind.OwnedEntity != null)
            {
                var player = new List<EntityUid>
                {
                    mind.OwnedEntity.Value
                };
                GiveHeadRev(player, RevolutionaryAntagRole, revRule);
            }
            if (mind.Session != null)
            {
                var message = Loc.GetString("head-rev-role-greeting");
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.FromHex("#5e9cff"));
            }
        }
    }
    private void OnCommandMobStateChanged(EntityUid uid, CommandStaffComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckCommandLose(false);
    }

    private void OnCommandGridChanged(EntityUid uid, CommandStaffComponent comp, ChangedGridEvent ev)
    {
        CheckCommandLose(false);
    }

    /// <summary>
    /// Checks if all of command is dead and if so will remove all sec and command jobs if there were any left.
    /// </summary>
    private bool CheckCommandLose(bool roundEnd)
    {
        var commandList = new List<EntityUid>();

        var heads = AllEntityQuery<CommandStaffComponent>();
        while (heads.MoveNext(out var id, out _))
        {
            commandList.Add(id);
        }

        if (roundEnd)
            return (_antagSelection.IsGroupDead(commandList, false, true));

        else if (_antagSelection.IsGroupDead(commandList, true, false))
        {
            RoundEnd();
            return true;
        }
        else return false;
    }

    private void OnHeadRevMobStateChanged(EntityUid uid, HeadRevolutionaryComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckRevsLose(false);
    }

    private void OnHeadRevGridChanged(EntityUid uid, HeadRevolutionaryComponent comp, ChangedGridEvent ev)
    {
        CheckRevsLose(false);
    }

    /// <summary>
    /// Checks if all the Head Revs are dead and if so will deconvert all regular revs.
    /// </summary>
    private bool CheckRevsLose(bool roundEnd)
    {
        var stunTime = TimeSpan.FromSeconds(4);
        var headRevList = new List<EntityUid>();

        var headRevs = AllEntityQuery<HeadRevolutionaryComponent, MobStateComponent>();
        while (headRevs.MoveNext(out var uid, out _, out _))
        {
            headRevList.Add(uid);
        }

        if (roundEnd)
            return (_antagSelection.IsGroupDead(headRevList, false, true));

        // If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
        else if (_antagSelection.IsGroupDead(headRevList, true, false))
        {
            var rev = AllEntityQuery<RevolutionaryComponent>();
            while (rev.MoveNext(out var uid, out _))
            {
                if (!HasComp<HeadRevolutionaryComponent>(uid))
                {
                    _stun.TryParalyze(uid, stunTime, true);
                    RemCompDeferred<RevolutionaryComponent>(uid);
                    _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", Identity.Entity(uid, EntityManager))), uid);
                    _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to all Head Revolutionaries dying.");

                    if (_mind.TryGetMind(uid, out var mindId, out var _))
                    {
                        _role.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId);
                    }
                }
            }
            RoundEnd();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Called when all Command or all Head Revs die or are exiled to end the round
    /// </summary>
    private void RoundEnd()
    {
        if (!_roundEnd.IsRoundEndRequested())
        {
            var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
            while (query.MoveNext(out var uid, out var revRule, out var gameRule))
            {
                _roundEnd.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, revRule.ShuttleCallTime);
                GameTicker.EndGameRule(uid, gameRule);
            }
        }
    }

    /// <summary>
    /// On cloning of a Head Rev, It will give new new body the components and remove them off the old one.
    /// </summary>
    private void OnClone(EntityUid rev, RevolutionaryComponent comp, CloningEvent ev)
    {
        if (HasComp<HeadRevolutionaryComponent>(ev.Source))
        {
            RemComp<HeadRevolutionaryComponent>(ev.Source);
            RemComp<RevolutionaryComponent>(ev.Source);
            AddComp<RevolutionaryComponent>(ev.Target);
            AddComp<HeadRevolutionaryComponent>(ev.Target);
        }
        else
        {
            RemComp<RevolutionaryComponent>(ev.Source);
            AddComp<RevolutionaryComponent>(ev.Target);
        }
        
    }

    private static readonly string[] Outcomes =
    {
        // revs survived and heads survived... how
        "rev-reverse-stalemate",
        // revs won and heads died
        "rev-won",
        // revs lost and heads survived
        "rev-lost",
        // revs lost and heads died
        "rev-stalemate"
    };
}
