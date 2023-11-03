using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Flash;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Server.Unitology.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Unitology.Components;
using Content.Shared.Roles;
using Content.Shared.Stunnable;
using Content.Shared.InfectionDead.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Humanoid;
using Content.Server.Abilities.Unitolog;
using Content.Shared.Sanity.Components;
using Content.Shared.Tag;
using Content.Shared.Mobs.Components;
using Content.Server.Database;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Where all the main stuff for Uniolutionaries happens (Assigning  Unis, Command on station, and checking for the game to end.)
/// </summary>
public sealed class UnitologyRuleSystem : GameRuleSystem<UnitologyRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _Tag = default!;



    [ValidatePrototypeId<NpcFactionPrototype>]
    public const string UnitologyNpcFaction = "Necromorfs";
    [ValidatePrototypeId<AntagPrototype>]
    public const string UnitologyAntagRole = "Uni";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<UnitologyRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    protected override void Started(EntityUid uid, UnitologyRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        component.CommandCheck = _timing.CurTime + component.TimerWait;
    }


    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var lost = result();
        var query = AllEntityQuery<UnitologyRuleComponent>();
        var index = 0;
        if(lost)
        { index = 1; }
        else
        { index = 2; }

        while (query.MoveNext(out var uni))
        {
            ev.AddLine(Loc.GetString(Outcomes[index]));

            ev.AddLine(Loc.GetString("uni-initial-count", ("initialCount", uni.Unis.Count)));
            foreach (var player in uni.Unis)
            {
                _mind.TryGetSession(player.Value, out var session);
                var username = session?.Name;
                if (username != null)
                {
                    ev.AddLine(Loc.GetString("uni-initial-name-user",
                    ("name", player.Key),
                    ("username", username)));
                }
                else
                {
                    ev.AddLine(Loc.GetString("uni-initial-name",
                    ("name", player.Key)));
                }
            }
            break;
        }
    }

    private void OnGetBriefing(EntityUid uid, UnitologyRoleComponent comp, ref GetBriefingEvent args)
    {
        if (!TryComp<MindComponent>(uid, out var mind) || mind.OwnedEntity == null)
            return;

        args.Append(Loc.GetString("uni-briefing"));
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = AllEntityQuery<UnitologyRuleComponent, GameRuleComponent>();
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
            _antagSelection.EligiblePlayers(comp.UniPrototypeId, comp.MaxUnis, comp.PlayersPerUni, comp.UniStartSound,
                "uni-role-greeting", "#5e9cff", out var chosen);
            if (chosen.Any())
                GiveUni(chosen, comp.UniPrototypeId, comp);
            else
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("uni-no-heads"));
            }
        }
    }

    private void GiveUni(List<EntityUid> chosen, string antagProto, UnitologyRuleComponent comp)
    {
        foreach (var Uni in chosen)
        {
            RemComp<CultStaffComponent>(Uni);

            var inCharacterName = MetaData(Uni).EntityName;
            if (_mind.TryGetMind(Uni, out var mindId, out var mind))
            {
                if (!_role.MindHasRole<UnitologyRoleComponent>(mindId))
                {
                    _role.MindAddRole(mindId, new UnitologyRoleComponent { PrototypeId = antagProto });
                }
                if (mind.Session != null)
                {
                    comp.Unis.Add(inCharacterName, mindId);
                }
            }

            _antagSelection.GiveAntagBagGear(Uni, comp.StartingGear);

            //_Tag.AddTag(Uni, "Flesh");
            EnsureComp<UnitologyComponent>(Uni);
            EnsureComp<ImmunitetInfectionDeadComponent>(Uni);
            EnsureComp<UnitologTileSpawnComponent>(Uni);
            RemCompDeferred<SanityComponent>(Uni);

            if(!comp.ObeliskState)
            {EnsureComp<UnitologPowersComponent>(Uni);}
            comp.ObeliskState = true;
            var factionComp = EnsureComp<NpcFactionMemberComponent>(Uni);
            foreach (var id in new List<string>(factionComp.Factions))
            {
                _npcFaction.RemoveFaction(Uni, id);
            }
            _npcFaction.AddFaction(Uni, UnitologyNpcFaction);
        }
    }

    public void OnUniAdmin(EntityUid mindId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return;

        var uniRule = EntityQuery<UnitologyRuleComponent>().FirstOrDefault();
        if (uniRule == null)
        {
            GameTicker.StartGameRule("Unitology", out var ruleEnt);
            uniRule = Comp<UnitologyRuleComponent>(ruleEnt);
        }

        if (!HasComp<UnitologyComponent>(mind.OwnedEntity))
        {
            if (mind.OwnedEntity != null)
            {
                var player = new List<EntityUid>
                {
                    mind.OwnedEntity.Value
                };
                GiveUni(player, UnitologyAntagRole, uniRule);
            }
            if (mind.Session != null)
            {
                var message = Loc.GetString("uni-role-greeting");
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, mind.Session.ConnectedClient, Color.FromHex("#5e9cff"));
            }
        }
    }


    private bool result()
    {
        var humRevs = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent>();
        int humanCount = 0;
        while (humRevs.MoveNext(out var uid, out _, out _))
        {
            if (!_mobState.IsDead(uid))
            {humanCount+= 1;}
        }

        var uniRevs = AllEntityQuery<UnitologyComponent, MobStateComponent>();
        int uniCount = 0;
        while (uniRevs.MoveNext(out var uid, out _, out _))
        {
            if (!_mobState.IsDead(uid))
            {uniCount+= 1;}
        }

        if (humanCount < uniCount)
        {
        return true;
        }
        return false;

    }

    private static readonly string[] Outcomes =
    {
        "uni-stalemate",
        "uni-won",
        "uni-lost"
    };
}
