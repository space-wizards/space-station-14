using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Administration.Systems;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Communications;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Implants;
using Content.Server.Mind;
using Content.Server.Pinpointer;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.GameTicking.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Manages the "Suspicion on the Space Station" gamemode. TTT but in space station 14.
/// </summary>
public sealed partial class SuspicionRuleSystem : GameRuleSystem<SuspicionRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelectionSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SubdermalImplantSystem _subdermalImplant = default!;
    [Dependency] private readonly AccessSystem _accessSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StoreSystem _storeSystem = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private readonly NavMapSystem _navMapSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;

    private readonly SoundSpecifier _traitorStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<SuspicionRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<SuspicionPlayerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SuspicionPlayerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCall);
    }


    protected override void AppendRoundEndText(EntityUid uid,
        SuspicionRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var traitors = FindAllOfType(SuspicionRole.Traitor, false);
        var innocents = FindAllOfType(SuspicionRole.Innocent, false);
        var detectives = FindAllOfType(SuspicionRole.Detective, false);

        var traitorsOnlyAlive = FindAllOfType(SuspicionRole.Traitor);
        var innocentsOnlyAlive = FindAllOfType(SuspicionRole.Innocent);
        var detectivesOnlyAlive = FindAllOfType(SuspicionRole.Detective);

        void Append(List<EntityUid> people, ref RoundEndTextAppendEvent args)
        {
            foreach (var person in people)
            {
                var name = MetaData(person).EntityName;
                var isDead = _mobState.IsDead(person);
                if (isDead)
                    args.AddLine($"[bullet/] {name} (Dead)");
                else
                    args.AddLine($"[bullet/] {name}");
            }
        }

        var victory = innocentsOnlyAlive.Count + detectivesOnlyAlive.Count == 0 ? "Traitors" : "Innocents";
        // Traitors win if there are no innocents or detectives left.

        args.AddLine($"[bold]{victory}[/bold] have won the round.");

        args.AddLine($"[color=red][bold]Traitors[/bold][/color]: {traitors.Count}");
        Append(traitors.Select(t => t.body).ToList(), ref args);
        args.AddLine($"[color=green][bold]Innocents[/bold][/color]: {innocents.Count}");
        Append(innocents.Select(t => t.body).ToList(), ref args);
        args.AddLine($"[color=blue][bold]Detectives[/bold][/color]: {detectives.Count}");
        Append(detectives.Select(t => t.body).ToList(), ref args);
    }


    protected override void Started(EntityUid uid, SuspicionRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.GameState = SuspicionGameState.Preparing;

        Timer.Spawn(TimeSpan.FromSeconds(component.PreparingDuration - 5), () =>  _chatManager.DispatchServerAnnouncement("The round will start in 5 seconds."));
        Timer.Spawn(TimeSpan.FromSeconds(component.PreparingDuration), () =>  StartRound(uid, component, gameRule));
        Log.Debug("Starting a game of Suspicion.");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SuspicionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var sus, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (sus.GameState != SuspicionGameState.InProgress)
                continue;

            sus.EndAt -= TimeSpan.FromSeconds(frameTime);

            var timeLeft = sus.EndAt.TotalSeconds;
            switch (timeLeft)
            {
                case <= 240 when !sus.AnnouncedTimeLeft.Contains(240):
                    _chatManager.DispatchServerAnnouncement($"The round will end in {Math.Round(sus.EndAt.TotalMinutes)}:{sus.EndAt.Seconds}.");
                    sus.AnnouncedTimeLeft.Add(240);
                    break;
                case <= 180 when !sus.AnnouncedTimeLeft.Contains(180):
                    _chatManager.DispatchServerAnnouncement($"The round will end in {Math.Round(sus.EndAt.TotalMinutes)}:{sus.EndAt.Seconds}.");
                    sus.AnnouncedTimeLeft.Add(180);
                    break;
                case <= 120 when !sus.AnnouncedTimeLeft.Contains(120):
                    _chatManager.DispatchServerAnnouncement($"The round will end in {Math.Round(sus.EndAt.TotalMinutes)}:{sus.EndAt.Seconds}.");
                    sus.AnnouncedTimeLeft.Add(120);
                    break;
                case <= 60 when !sus.AnnouncedTimeLeft.Contains(60):
                    _chatManager.DispatchServerAnnouncement($"The round will end in {Math.Round(sus.EndAt.TotalMinutes)}:{sus.EndAt.Seconds}.");
                    sus.AnnouncedTimeLeft.Add(60);
                    break;
                case <= 30 when !sus.AnnouncedTimeLeft.Contains(30):
                    _chatManager.DispatchServerAnnouncement($"The round will end in 30 seconds.");
                    sus.AnnouncedTimeLeft.Add(30);
                    break;
                case <= 10 when !sus.AnnouncedTimeLeft.Contains(10):
                    _chatManager.DispatchServerAnnouncement($"The round will end in 10 seconds.");
                    sus.AnnouncedTimeLeft.Add(10);
                    break;
                case <= 5 when !sus.AnnouncedTimeLeft.Contains(5):
                    _chatManager.DispatchServerAnnouncement($"The round will end in 5 seconds.");
                    sus.AnnouncedTimeLeft.Add(5);
                    break;
            }

            if (sus.EndAt <= TimeSpan.Zero)
            {
                sus.GameState = SuspicionGameState.PostRound;
                _roundEndSystem.EndRound(TimeSpan.FromSeconds(sus.PostRoundDuration));
                return;
            }
        }
    }
}
