// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.DeadSpace.Renegade.Components;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.Audio;
using Content.Shared.DeadSpace.Renegade.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Renegade;

public sealed class RenegadeRule : StationEventSystem<Renegade.Components.RenegadeRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);
    }

    protected override void Added(EntityUid uid, Renegade.Components.RenegadeRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
    }

    protected override void AppendRoundEndText(EntityUid uid, Renegade.Components.RenegadeRuleComponent component, GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var sessionData = _antag.GetAntagIdentifiers(uid);

        foreach (var (mind, data, name) in sessionData)
        {
            _role.MindHasRole<RenegadeSubmissionConditionComponent>(mind, out var role);
            var count = CompOrNull<RenegadeSubmissionConditionComponent>(role)?.SubordinateCommand.Count ?? 0;

            args.AddLine(Loc.GetString("renegade-sub-name-user",
            ("name", name),
            ("username", data.UserName),
            ("count", count)));
        }
    }

    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        bool isConditionsComplete = false;

        var queryRenegade = EntityQueryEnumerator<RenegadeComponent>();
        EntityUid RenegadeEntity = default;

        while (queryRenegade.MoveNext(out var RenegadeEnt, out _))
        {
            if (!_mindSystem.TryGetMind(RenegadeEnt, out var mindId, out var mind))
                continue;

            if (mind == null)
                continue;

            foreach (var objId in mind.Objectives)
            {
                if (!HasComp<RenegadeSubmissionConditionComponent>(objId))
                    continue;

                if (_objectives.IsCompleted(objId, (mindId, mind)))
                {
                    isConditionsComplete = true;
                    RenegadeEntity = RenegadeEnt;
                    break;
                }

            }

            if (isConditionsComplete)
                break;
        }

        if (!isConditionsComplete)
            return;

        var msg = new GameGlobalSoundEvent("/Audio/_DeadSpace/Renegade/the_Renegade_has_captured_the_space_station.ogg", AudioParams.Default);
        var stationFilter = _stationSystem.GetInOwningStation(RenegadeEntity);
        stationFilter.AddPlayersByPvs(RenegadeEntity, entityManager: EntityManager);
        RaiseNetworkEvent(msg, stationFilter);

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("round-end-system-Renegade-shuttle-called-announcement"), playSound: true, colorOverride: Color.DarkRed);
    }
}
