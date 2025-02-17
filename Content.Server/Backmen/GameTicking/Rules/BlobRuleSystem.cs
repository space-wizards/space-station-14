using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.AlertLevel;
using Content.Server.Backmen.Blob.Rule;
using Content.Server.Backmen.GameTicking.Rules.Components;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Nuke;
using Content.Server.Objectives;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.Audio;

namespace Content.Server.Backmen.GameTicking.Rules;

public sealed class BlobRuleSystem : GameRuleSystem<BlobRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly NukeCodePaperSystem _nukeCode = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectivesSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private static readonly SoundPathSpecifier BlobDetectAudio = new SoundPathSpecifier("/Audio/_DeadSpace/Announcements/outbreak5.ogg"); // DS14-Announcements

    public override void Initialize()
    {
        base.Initialize();
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

            if (comp.BlobTiles.Count >= 50)
            {
                if (_roundEndSystem.ExpectedCountdownEnd != null)
                {
                    _roundEndSystem.CancelRoundEndCountdown(checkCooldown: false);
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
                return;
            case BlobStage.Begin when blobCore.Comp.BlobTiles.Count >= 500:
                {
                    blobRuleComp.Stage = BlobStage.Critical;
                    _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("blob-alert-critical"),
                        Loc.GetString("Station"),
                        true,
                        blobRuleComp.AlertAudio,
                        Color.Red);

                    _nukeCode.SendNukeCodes(stationUid);

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
            case BlobStage.Critical when blobCore.Comp.BlobTiles.Count >= 1400:
                {
                    blobRuleComp.Stage = BlobStage.TheEnd;
                    _roundEndSystem.EndRound();

                    RaiseLocalEvent(stationUid, new BlobChangeLevelEvent
                    {
                        BlobCore = blobCore,
                        Station = stationUid,
                        Level = blobRuleComp.Stage
                    }, broadcast: true);
                    return;
                }
        }
    }

    protected override void AppendRoundEndText(EntityUid uid, BlobRuleComponent blob, GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent ev)
    {
        if (blob.Blobs.Count < 1)
            return;

        var result = Loc.GetString("blob-round-end-result", ("blobCount", blob.Blobs.Count));

        // yeah this is duplicated from traitor rules lol, there needs to be a generic rewrite where it just goes through all minds with objectives
        foreach (var (mindId, mind) in blob.Blobs)
        {
            var name = mind.CharacterName;
            _mindSystem.TryGetSession(mindId, out var session);
            var username = session?.Name;

            var objectives = mind.AllObjectives.ToArray();
            if (objectives.Length == 0)
            {
                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("blob-user-was-a-blob", ("user", username));
                    else
                    {
                        result += "\n" + Loc.GetString("blob-user-was-a-blob-named", ("user", username),
                            ("name", name));
                    }
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("blob-was-a-blob-named", ("name", name));

                continue;
            }

            if (username != null)
            {
                if (name == null)
                {
                    result += "\n" + Loc.GetString("blob-user-was-a-blob-with-objectives",
                        ("user", username));
                }
                else
                {
                    result += "\n" + Loc.GetString("blob-user-was-a-blob-with-objectives-named",
                        ("user", username), ("name", name));
                }
            }
            else if (name != null)
                result += "\n" + Loc.GetString("blob-was-a-blob-with-objectives-named", ("name", name));

            foreach (var objectiveGroup in objectives.GroupBy(o => Comp<ObjectiveComponent>(o).LocIssuer))
            {
                foreach (var objective in objectiveGroup)
                {
                    var info = _objectivesSystem.GetInfo(objective, mindId, mind);
                    if (info == null)
                        continue;

                    var objectiveTitle = info.Value.Title;
                    var progress = info.Value.Progress;

                    if (progress > 0.99f)
                    {
                        result += "\n- " + Loc.GetString(
                            "objective-condition-success",
                            ("condition", objectiveTitle),
                            ("markupColor", "green")
                        );
                    }
                    else
                    {
                        result += "\n- " + Loc.GetString(
                            "objective-condition-fail",
                            ("condition", objectiveTitle),
                            ("progress", (int) (progress * 100)),
                            ("markupColor", "red")
                        );
                    }
                }
            }
        }

        ev.AddLine(result);
    }
}
