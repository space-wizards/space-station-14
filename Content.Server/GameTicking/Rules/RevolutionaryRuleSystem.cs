using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Content.Shared.Stunnable;
using Content.Server.Shuttles.Components;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Shared.IdentityManagement;
using Content.Server.Flash;
using Content.Shared.Mindshield.Components;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Antag;
using Content.Server.NPC.Components;
using Content.Server.Roles.Jobs;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Zombies;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Revolutionaries happens (Assigning Head Revs, Command on station, and checking for the game to end.)
/// </summary>
public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    [ValidatePrototypeId<DepartmentPrototype>]
    public const string CommandDepartmentId = "Command";
    [ValidatePrototypeId<NpcFactionPrototype>]
    public const string RevolutionaryNpcFaction = "Revolutionary";
    [ValidatePrototypeId<AntagPrototype>]
    public const string RevolutionaryAntagRole = "Rev";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<CommandStaffComponent, MobStateChangedEvent>(OnCommandMobStateChanged);
        SubscribeLocalEvent<HeadRevolutionaryComponent, MobStateChangedEvent>(OnHeadRevMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<HeadRevolutionaryComponent, AfterFlashedEvent>(OnPostFlash);
    }

    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        component.CommandCheck = _timing.CurTime + component.TimerWait;
    }

    /// <summary>
    /// Checks if the round should end and also checks who has a mindshield.
    /// </summary>
    protected override void ActiveTick(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        if (GameTicker.IsGameRuleAdded(uid, gameRule) && component.CommandCheck <= _timing.CurTime)
        {
            component.CommandCheck = _timing.CurTime + component.TimerWait;
            CheckCommandLose();
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var revsLost = CheckRevsLose();
        var commandLost = CheckCommandLose();
        var query = AllEntityQuery<RevolutionaryRuleComponent>();
        while (query.MoveNext(out var headrev))
        {
            // This is (revsLost, commandsLost) concatted together
            // (moony wrote this comment idk what it means)
            var index = (commandLost ? 1 : 0) | (revsLost ? 2 : 0);
            ev.AddLine(Loc.GetString(Outcomes[index]));

            ev.AddLine(Loc.GetString("head-rev-initial-count", ("initialCount", headrev.HeadRevs.Count)));
            foreach (var player in headrev.HeadRevs)
            {
                _mind.TryGetSession(player.Value, out var session);
                var username = session?.Name;
                if (username != null)
                {
                    ev.AddLine(Loc.GetString("head-rev-initial",
                    ("name", player.Key),
                    ("username", username)));
                }
                else
                {
                    ev.AddLine(Loc.GetString("head-rev-initial",
                    ("name", player.Key)));
                }
            }
            break;
        }
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
        var query = AllEntityQuery<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            InitialAssignCommandStaff();

            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            _antagSelection.EligiblePlayers(comp.RevPrototypeId, comp.MaxHeadRevs, comp.PlayersPerHeadRev, comp.HeadRevStartSound,
                "head-rev-role-greeting", "#5e9cff", out var chosen);
            GiveHeadRev(chosen, comp.RevPrototypeId, comp);
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
    /// At start gets all the initial players to check for heads
    /// </summary>
    private void InitialAssignCommandStaff()
    {
        var playerList = new List<EntityUid>();
        var query = AllEntityQuery<MindContainerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!HasComp<HumanoidAppearanceComponent>(uid) || !comp.HasMind)
                return;
            playerList.Add(uid);
        }
        AssignCommandStaff(playerList);
    }

    /// <summary>
    /// Gives heads the command component so they are tracked for Revs.
    /// </summary>
    /// <param name="playerList"></param>
    /// <param name="latejoin"></param>
    private void AssignCommandStaff(List<EntityUid> playerList, bool latejoin = false)
    {
        var currentCommandStaff = 0;
        var commandDepartment = _prototypeManager.Index<DepartmentPrototype>(CommandDepartmentId);
        foreach (var player in playerList)
        {
            if (!_mind.TryGetMind(player, out var mindId, out var mind))
                continue;
            if (!_job.MindTryGetJob(mindId, out _, out var jobProto) ||
                !commandDepartment.Roles.Contains(jobProto.ID) ||
                mind.OwnedEntity == null)
                continue;
            if (HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity))
                continue;

            EnsureComp<CommandStaffComponent>(mind.OwnedEntity.Value);
            currentCommandStaff++;
        }

        if (latejoin)
            return;

        if (currentCommandStaff == 0)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out var uid, out _, out _, out var gameRule))
            {
                GameTicker.EndGameRule(uid, gameRule);
            }
        }
    }

    /// <summary>
    /// Called when a Head Rev uses a flash in melee to convert somebody else.
    /// </summary>
    public void OnPostFlash(EntityUid uid, HeadRevolutionaryComponent comp, ref AfterFlashedEvent ev)
    {
        var stunTime = comp.StunTime;
        if (HasComp<RevolutionaryComponent>(ev.Target) ||
            HasComp<MindShieldComponent>(ev.Target) ||
            (!HasComp<HumanoidAppearanceComponent>(ev.Target) &&
             !HasComp<AlwaysRevolutionaryConvertibleComponent>(ev.Target)) ||
            !_mobState.IsAlive(ev.Target) ||
            HasComp<ZombieComponent>(ev.Target))
        {
            return;
        }

        _npcFaction.AddFaction(ev.Target, RevolutionaryNpcFaction);
        EnsureComp<RevolutionaryComponent>(ev.Target);
        _stun.TryParalyze(ev.Target, stunTime, true);
        if (ev.User != null)
        {
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(ev.User.Value)} converted {ToPrettyString(ev.Target)} into a Revolutionary");
        }
        if (_mind.TryGetMind(ev.Target, out var mindId, out var mind))
        {
            if (!_role.MindHasRole<RevolutionaryRoleComponent>(mindId))
            {
                _role.MindAddRole(mindId, new RevolutionaryRoleComponent { PrototypeId = RevolutionaryAntagRole });
            }
            if (mind.Session != null)
            {
                var message = Loc.GetString("rev-role-greeting");
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.Red);
            }
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
                InitialAssignCommandStaff();
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
            CheckCommandLose();
    }

    /// <summary>
    /// Checks if all of command is dead and if so will remove all sec and command jobs if there were any left.
    /// </summary>
    private bool CheckCommandLose()
    {
        var commandList = new List<EntityUid>();

        var heads = AllEntityQuery<CommandStaffComponent>();
        while (heads.MoveNext(out var id, out _))
        {
            commandList.Add(id);
        }

        return _antagSelection.IsGroupDead(commandList, true);
    }

    private void OnHeadRevMobStateChanged(EntityUid uid, HeadRevolutionaryComponent comp, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead || ev.NewMobState == MobState.Invalid)
            CheckRevsLose();
    }

    /// <summary>
    /// Checks if all the Head Revs are dead and if so will deconvert all regular revs.
    /// </summary>
    private bool CheckRevsLose()
    {
        var stunTime = TimeSpan.FromSeconds(4);
        var headRevList = new List<EntityUid>();

        var headRevs = AllEntityQuery<HeadRevolutionaryComponent, MobStateComponent>();
        while (headRevs.MoveNext(out var uid, out _, out _))
        {
            headRevList.Add(uid);
        }

        // If no Head Revs are alive all normal Revs will lose their Rev status and rejoin Nanotrasen
        if (_antagSelection.IsGroupDead(headRevList, false))
        {
            var rev = AllEntityQuery<RevolutionaryComponent>();
            while (rev.MoveNext(out var uid, out _))
            {
                if (!HasComp<HeadRevolutionaryComponent>(uid))
                {
                    _npcFaction.RemoveFaction(uid, RevolutionaryNpcFaction);
                    _stun.TryParalyze(uid, stunTime, true);
                    RemCompDeferred<RevolutionaryComponent>(uid);
                    _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", Identity.Entity(uid, EntityManager))), uid);
                    _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to all Head Revolutionaries dying.");
                }
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gives late join heads the head component so they also need to be killed.
    /// </summary>
    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (_mind.TryGetMind(ev.Player, out _, out var mind) &&
            HasComp<PendingClockInComponent>(mind.OwnedEntity) &&
            ev.Player.AttachedEntity != null)
        {
            var list = new List<EntityUid>
            {
                ev.Player.AttachedEntity.Value
            };
            AssignCommandStaff(list, true);
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
