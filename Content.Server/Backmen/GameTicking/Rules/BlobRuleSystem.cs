using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Managers; // DS14
using Content.Server.AlertLevel;
using Content.Server.Backmen.Blob.Rule;
using Content.Server.Backmen.GameTicking.Rules.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Database;
using Content.Server.DeadSpace.Nuke;
using Content.Server.DeadSpace.ERT;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Administration; // DS14
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.DeadSpace.ERT.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.DeadSpace.Nuke;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Objectives.Components;
using Robust.Server.Player;
using Content.Server.Objectives;
using Robust.Shared.Network;

namespace Content.Server.Backmen.GameTicking.Rules;

public sealed class BlobRuleSystem : GameRuleSystem<BlobRuleComponent>
{
    [Dependency] private readonly IAdminManager _admin = default!; // DS14
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly NukeCodeSendQueueSystem _nukeCodeQueue = default!; // DS14
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectivesSystem = default!;
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ErtResponseSystem _ertResponseSystem = default!; // DS14
    [Dependency] private readonly IServerDbManager _db = default!;
    private static readonly ProtoId<ErtTeamPrototype> ErtTeam = "CburnSierra";
    private static readonly ProtoId<CargoAccountPrototype> Account = "Security";
    private const int AdditionalSupport = 70000;
    private const int BlobVictoryTiles = 1400; // DS14
    private bool _helpSended = false;
    private static readonly SoundPathSpecifier BlobDetectAudio = new SoundPathSpecifier("/Audio/_DeadSpace/Announcements/outbreak5.ogg"); // DS14-Announcements

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _helpSended = false;
    }

    protected override void Started(EntityUid uid, BlobRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        var activeRules = QueryActiveRules();
        while (activeRules.MoveNext(out var entityUid, out _, out _, out _))
        {
            if (uid == entityUid)
                continue;

            GameTicker.EndGameRule(uid, gameRule);
            Log.Error("blob is active!!! remove!");
            break;
        }
    }

    protected override void ActiveTick(EntityUid uid, BlobRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        component.Accumulator += frameTime;

        if (component.Accumulator < 10)
            return;

        component.Accumulator = 0;

        var blobCoreQuery = EntityQueryEnumerator<BlobCoreComponent, MetaDataComponent>();
        while (blobCoreQuery.MoveNext(out var ent, out var comp, out _))
        {
            if (TerminatingOrDeleted(ent))
            {
                continue;
            }

            if (component.Stage != BlobStage.TheEnd && comp.BlobTiles.Count >= 50) // DS14
            {
                if (_roundEndSystem.ExpectedCountdownEnd != null)
                {
                    _roundEndSystem.CancelRoundEndCountdown(forceRecall: true);
                    _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("blob-alert-recall-shuttle"),
                        Loc.GetString("Station"),
                        false,
                        null,
                        Color.Red);
                }
            }

            if (!CheckBlobInStation(ent, out var stationUid))
            {
                continue;
            }

            CheckChangeStage((ent, comp), stationUid.Value, component);
        }
    }

    // DS14-start
    protected override void AppendAdminStatus(EntityUid uid,
        BlobRuleComponent component,
        GameRuleComponent gameRule,
        CollectGameRuleAdminStatusEvent args)
    {
        var stage = Loc.GetString(
            $"game-rule-admin-status-blob-stage-{component.Stage.ToString().ToLowerInvariant()}");
        var lines = new List<string>
        {
            Loc.GetString(
                "game-rule-admin-status-blob-summary",
                ("stage", stage),
                ("players", component.Blobs.Count)),
        };

        var cores = new List<(EntityUid? Station, int Tiles)>();
        var query = EntityQueryEnumerator<BlobCoreComponent>();
        while (query.MoveNext(out var core, out var coreComponent))
        {
            if (TerminatingOrDeleted(core))
                continue;

            cores.Add((_stationSystem.GetOwningStation(core), coreComponent.BlobTiles.Count));
        }

        foreach (var stationCores in cores.GroupBy(core => core.Station))
        {
            var coreCount = stationCores.Count();
            var totalTiles = stationCores.Sum(core => core.Tiles);
            var largestCore = stationCores.Max(core => core.Tiles);
            var progress = Math.Clamp(largestCore / (float) BlobVictoryTiles, 0f, 1f);
            var station = stationCores.Key is { } stationUid
                ? ToPrettyString(stationUid).Name ?? stationUid.ToString()
                : Loc.GetString("game-rule-admin-status-off-station");

            lines.Add(Loc.GetString(
                "game-rule-admin-status-blob-station",
                ("station", station),
                ("cores", coreCount),
                ("tiles", totalTiles),
                ("largest", largestCore),
                ("target", BlobVictoryTiles),
                ("progress", progress.ToString("P0"))));
        }

        if (cores.Count == 0)
            lines.Add(Loc.GetString("game-rule-admin-status-blob-no-cores"));

        args.AddSection(Loc.GetString("game-rule-admin-status-blob-title"), lines);
    }
    // DS14-end

    private bool CheckBlobInStation(EntityUid blobCore, [NotNullWhen(true)] out EntityUid? stationUid)
    {
        var station = _stationSystem.GetOwningStation(blobCore);
        if (station == null || !HasComp<StationEventEligibleComponent>(station.Value))
        {
            _chatManager.SendAdminAlert(blobCore, Loc.GetString("blob-alert-out-off-station"));
            QueueDel(blobCore);
            stationUid = null;
            return false;
        }

        stationUid = station.Value;
        return true;
    }

    private void CheckChangeStage(Entity<BlobCoreComponent> blobCore, EntityUid stationUid, BlobRuleComponent blobRuleComp)
    {
        switch (blobRuleComp.Stage)
        {
            case BlobStage.Default when blobCore.Comp.BlobTiles.Count > 20:
                blobRuleComp.Stage = BlobStage.Begin;

                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("blob-alert-detect"),
                    Loc.GetString("Station"), true, BlobDetectAudio, Color.Red);

                if (_alertLevel.GetLevel(stationUid) == "green" || _alertLevel.GetLevel(stationUid) == "blue" || _alertLevel.GetLevel(stationUid) == "violet" || _alertLevel.GetLevel(stationUid) == "yellow")
                    _alertLevel.SetLevel(stationUid, "red", false, true, true, true);

                RaiseLocalEvent(stationUid, new BlobChangeLevelEvent
                {
                    BlobCore = blobCore,
                    Station = stationUid,
                    Level = blobRuleComp.Stage
                }, broadcast: true);

                if (_helpSended)
                    return;

                if (!TryComp<StationBankAccountComponent>(stationUid, out var stationAccount))
                    return;

                var addMoneyAfterWarDeclared = _ertResponseSystem.GetErtPrice(ErtTeam) + AdditionalSupport; // DS14

                _cargoSystem.UpdateBankAccount(
                                    (stationUid, stationAccount),
                                    addMoneyAfterWarDeclared,
                                    Account
                                );

                _helpSended = true;

                return;
            case BlobStage.Begin when blobCore.Comp.BlobTiles.Count >= 500:
                {
                    blobRuleComp.Stage = BlobStage.Critical;
                    // DS14-Start: queue automatic nuke-code dispatches for admin review.
                    _nukeCodeQueue.TryQueueAutomaticRequest(
                        stationUid,
                        NukeCodeSendReasonIds.BlobCriticalMass,
                        out _);
                    // DS14-End

                    if (_alertLevel.GetLevel(stationUid) != "enigma" && _alertLevel.GetLevel(stationUid) != "delta" && _alertLevel.GetLevel(stationUid) != "epsilon")
                        _alertLevel.SetLevel(stationUid, "sierra", true, true, true, true);

                    RaiseLocalEvent(stationUid, new BlobChangeLevelEvent
                    {
                        BlobCore = blobCore,
                        Station = stationUid,
                        Level = blobRuleComp.Stage
                    }, broadcast: true);
                    return;
                }
            case BlobStage.Critical when blobCore.Comp.BlobTiles.Count >= BlobVictoryTiles: // DS14
                {
                    blobRuleComp.Stage = BlobStage.TheEnd;
                    // DS14: Record the victory without forcibly ending the round.

                    RaiseLocalEvent(stationUid, new BlobChangeLevelEvent
                    {
                        BlobCore = blobCore,
                        Station = stationUid,
                        Level = blobRuleComp.Stage
                    }, broadcast: true);

                    if (!HasActiveRoundAdmin()) // DS14
                        _roundEndSystem.EndRound();

                    return;
                }
        }
    }

    // DS14-start
    private bool HasActiveRoundAdmin()
    {
        foreach (var admin in _admin.ActiveAdmins)
        {
            if (_admin.HasAdminFlag(admin, AdminFlags.Round))
                return true;
        }

        return false;
    }
    // DS14-end

    protected override void AppendRoundEndText(EntityUid uid, BlobRuleComponent blob, GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent ev)
    {
        if (blob.Blobs.Count < 1)
            return;

        var result = Loc.GetString("blob-round-end-result", ("blobCount", blob.Blobs.Count));

        ev.AddLine(result);

        // DS14 Статистика для дашборда
        var winner = blob.Stage == BlobStage.TheEnd
            ? BiStatWinner.Antagonist
            : BiStatWinner.Crew;

        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                await _db.AddBiStatAsync("Блоб", winner, DateTime.UtcNow);
            }
            catch
            {

            }
        });
    }

    // DS14-start
    protected override void AppendRoundEndDiscordText(EntityUid uid,
        BlobRuleComponent blob,
        GameRuleComponent gameRule,
        ref RoundEndDiscordTextAppendEvent ev)
    {
        if (blob.Blobs.Count < 1)
            return;

        foreach (var (mindId, mind) in blob.Blobs)
        {
            var name = mind.CharacterName;
            var username = GetBlobUsername(mind.UserId ?? mind.OriginalOwnerUserId);

            var objectives = mind.Objectives.ToArray();
            if (objectives.Length == 0)
            {
                if (username != null)
                {
                    ev.AddLine(name == null
                        ? Loc.GetString("blob-user-was-a-blob", ("user", username))
                        : Loc.GetString("blob-user-was-a-blob-named", ("user", username), ("name", name)));
                }
                else if (name != null)
                    ev.AddLine(Loc.GetString("blob-was-a-blob-named", ("name", name)));

                continue;
            }

            if (username != null)
            {
                ev.AddLine(name == null
                    ? Loc.GetString("blob-user-was-a-blob-with-objectives", ("user", username))
                    : Loc.GetString("blob-user-was-a-blob-with-objectives-named", ("user", username), ("name", name)));
            }
            else if (name != null)
                ev.AddLine(Loc.GetString("blob-was-a-blob-with-objectives-named", ("name", name)));

            foreach (var objectiveGroup in objectives.GroupBy(o => Comp<ObjectiveComponent>(o).LocIssuer))
            {
                foreach (var objective in objectiveGroup)
                {
                    var info = _objectivesSystem.GetInfo(objective, mindId, mind);
                    if (info == null)
                        continue;

                    var objectiveTitle = info.Value.Title;
                    var progress = info.Value.Progress;

                    ev.AddLine(progress > 0.99f
                        ? "- " + Loc.GetString(
                            "objective-condition-success",
                            ("condition", objectiveTitle),
                            ("markupColor", "green"))
                        : "- " + Loc.GetString(
                            "objective-condition-fail",
                            ("condition", objectiveTitle),
                            ("progress", (int) (progress * 100)),
                            ("markupColor", "red")));
                }
            }
        }

        ev.AddLine("");
    }

    private string? GetBlobUsername(NetUserId? userId)
    {
        if (userId == null)
            return null;

        if (_player.TryGetSessionById(userId.Value, out var session))
            return session.Name;

        return _player.TryGetPlayerData(userId.Value, out var data)
            ? data.UserName
            : null;
    }
    // DS14-end
}
