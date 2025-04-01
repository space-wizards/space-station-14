using Content.Server.Administration.Commands;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Points;
using Robust.Server.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.Oddball;

// a lot of this is copied from DeathMatchRuleSystem. its march 31st and i have a single evening to shit this system out before april fools
public sealed class OddballRuleSystem : GameRuleSystem<OddballRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PointSystem _point = default!;
    [Dependency] private readonly RespawnRuleSystem _respawn = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<OddballRuleComponent, PlayerPointChangedEvent>(OnPointChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OddballRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var oddball, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule) ||
                _timing.CurTime < oddball.RoundEnd)
                continue;

            _roundEnd.EndRound(oddball.RestartDelay);
        }
    }

    protected override void Started(EntityUid uid, OddballRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var message = Loc.GetString("oddball-round-start", ("time", component.RoundDuration.Seconds));
        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToAll(ChatChannel.Server,
            message,
            wrapped,
            uid,
            false,
            true,
            Color.Gold);

        component.RoundEnd = _timing.CurTime + component.RoundDuration;
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var query = EntityQueryEnumerator<OddballRuleComponent, RespawnTrackerComponent, PointManagerComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var oddball, out var tracker, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var player = ev.Player.UserId;
            var newMind = _mind.CreateMind(player, ev.Profile.Name);
            _mind.SetUserId(newMind, player);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;

            _mind.TransferTo(newMind, mob);

            var gear = _random.Pick(oddball.SpawnGear);
            SetOutfitCommand.SetOutfit(mob, gear, EntityManager);

            _respawn.AddToTracker(player, (uid, tracker));

            _point.EnsurePlayer(player, uid, point);

            // Send respawn points message to spawned player
            if (oddball.Leader is { } leader && _player.TryGetPlayerData(leader, out var data))
            {
                var message = Loc.GetString(
                    player == leader ? "oddball-respawn-info-leader" : "oddball-respawn-info",
                    ("leader", data.UserName),
                    ("leaderPoints", point.Points[leader]),
                    ("playerPoints", point.Points[player]));
                var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chat.ChatMessageToOne(ChatChannel.Server,
                    message,
                    wrapped,
                    mob,
                    false,
                    ev.Player.Channel,
                    Color.LimeGreen);
            }

            ev.Handled = true;
            break;
        }
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<OddballRuleComponent, RespawnTrackerComponent, PointManagerComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var oddball, out var tracker, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            _respawn.AddToTracker((ev.Mob, null), (uid, tracker));
        }
    }

    private void OnPointChanged(Entity<OddballRuleComponent> ent, ref PlayerPointChangedEvent args)
    {
        if (ent.Comp.Leader == args.Player)
            return;

        var query = EntityQueryEnumerator<OddballRuleComponent, PointManagerComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out _, out var point, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var leaderPoints = ent.Comp.Leader is { } leader ? point.Points[leader] : 0;
            var playerPoints = point.Points[args.Player];

            if (playerPoints <= leaderPoints)
                continue;

            // Send "gained the lead" message to new leader
            if (_player.TryGetSessionById(args.Player, out var playerSession))
            {
                var message = Loc.GetString("oddball-lead-gained");
                var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chat.ChatMessageToOne(ChatChannel.Server,
                    message,
                    wrapped,
                    ent,
                    false,
                    playerSession.Channel,
                    Color.MediumTurquoise);
            }

            // Send "lost the lead" message to old leader
            if (_player.TryGetSessionById(ent.Comp.Leader, out var leaderSession))
            {
                var message = Loc.GetString("oddball-lead-lost");
                var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chat.ChatMessageToOne(ChatChannel.Server,
                    message,
                    wrapped,
                    ent,
                    false,
                    leaderSession.Channel,
                    Color.Red);
            }

            // Send new leader message to all players
            if (_player.TryGetPlayerData(args.Player, out var data))
            {
                var message = Loc.GetString("oddball-new-leader", ("leader", data.UserName), ("points", playerPoints));
                var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chat.ChatMessageToAll(ChatChannel.Server,
                    message,
                    wrapped,
                    ent,
                    false,
                    true,
                    Color.Gold);
            }

            ent.Comp.Leader = args.Player;
        }
    }

    protected override void AppendRoundEndText(EntityUid uid, OddballRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        if (!TryComp<PointManagerComponent>(uid, out var point))
            return;

        if (component.Leader is { } winner && _player.TryGetPlayerData(winner, out var data))
        {
            args.AddLine(Loc.GetString("point-scoreboard-winner", ("player", data.UserName)));
            args.AddLine("");
        }

        args.AddLine(Loc.GetString("point-scoreboard-header"));
        args.AddLine(new FormattedMessage(point.Scoreboard).ToMarkup());
    }
}
