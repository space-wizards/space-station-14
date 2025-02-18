// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.DeadSpace.ERTCall;

public sealed class ERTCallSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private ISawmill _sawmill = default!;

    private const string CallAnnouncementSound = "/Audio/_DeadSpace/Announcements/ERT/call.ogg";
    private const string DecisionAnnouncementSound = "/Audio/_DeadSpace/Announcements/ERT/decision.ogg";
    private const string RecallAnnouncementSound = "/Audio/_DeadSpace/Announcements/ERT/recall.ogg";
    private const string SpawnAnnouncementSound = "/Audio/_DeadSpace/Announcements/ERT/spawn.ogg";

    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
    }

    private void OnStationInitialize(StationInitializedEvent args)
    {
        if (!TryComp<ERTCallComponent>(args.Station, out var ertCallComponent))
            return;

        if (!_prototypeManager.TryIndex(ertCallComponent.ERTTeamPrototype, out ERTTeamPrototype? ertTeams))
        {
            return;
        }

        ertCallComponent.ERTTeams = ertTeams;
    }

    private bool CompliesRoundTime(EntityUid stationUid, ERTTeamDetail ertTeamDetail)
    {
        if ((_gameTiming.CurTime.TotalMinutes >= ertTeamDetail.RequiredRoundDuration))
            return true;

        return false;
    }

    public void CallErt(EntityUid stationUid, string ertTeam, MetaDataComponent? dataComponent = null, ERTCallComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ERTTeams == null
            || !component.ERTTeams.Teams.TryGetValue(ertTeam, out var ertTeamDetails)
           )
        {
            return;
        }

        var message = "";
        message += $"{Loc.GetString($"ert-call-announcement-{ertTeam}")} ";

        if (!CompliesRoundTime(stationUid, ertTeamDetails))
        {
            message += Loc.GetString("ert-call-round-time-refusal-announcement", ("name", Loc.GetString($"ert-team-name-{ertTeam}")),
                ("time", ertTeamDetails.RequiredRoundDuration));
            _chatSystem.DispatchGlobalAnnouncement(message, playSound: true, colorOverride: Color.Gold, announcementSound: new SoundPathSpecifier(CallAnnouncementSound));
            return;
        }

        if (component.NewCallCooldownRemaining > 0)
        {
            message += Loc.GetString("ert-call-cooldown-call-refusal-announcement", ("name", Loc.GetString($"ert-team-name-{ertTeam}")),
                ("time", component.TimeToAnotherSpawn.TotalMinutes));
            _chatSystem.DispatchGlobalAnnouncement(message, playSound: true, colorOverride: Color.Gold, announcementSound: new SoundPathSpecifier(CallAnnouncementSound));
            return;
        }

        component.ERTCalledTeam = ertTeamDetails;
        component.ERTCalled = true;
        component.WasApproved = ertTeamDetails.MustApproveAdmin ? false : true;
        component.ApproveCooldownRemaining = Convert.ToSingle(component.TimeToApprove.TotalSeconds);
        component.SpawnCooldownRemaining = Convert.ToSingle(ertTeamDetails.TimeToSpawn.TotalSeconds);
        component.AwaitsSpawn = false;

        message += Loc.GetString($"ert-call-wait-announcement", ("time", component.TimeToApprove.TotalMinutes));

        _chatSystem.DispatchGlobalAnnouncement(message, playSound: true, colorOverride: Color.Gold, announcementSound: new SoundPathSpecifier(CallAnnouncementSound));

        RaiseLocalEvent(new ERTCallEvent(stationUid));
    }

    public bool RecallERT(EntityUid stationUid, MetaDataComponent? dataComponent = null, ERTCallComponent? component = null)
    {
        if (!Resolve(stationUid, ref component, ref dataComponent)
            || component.ERTTeams == null
            || component.ERTCalledTeam == null)
        {
            return false;
        }

        if (!component.ERTCalled)
            return false;

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-recall-announcement", ("name", Loc.GetString(component.ERTCalledTeam.Name))),
            playSound: true, colorOverride: Color.Gold, announcementSound: new SoundPathSpecifier(RecallAnnouncementSound));

        component.ERTCalledTeam = null;
        component.ERTCalled = false;
        component.ApproveCooldownRemaining = 0;
        component.SpawnCooldownRemaining = 0;

        RaiseLocalEvent(new ERTRecallEvent(stationUid));

        return true;
    }

    public TimeSpan? TimeToErt(EntityUid? stationUid, MetaDataComponent? dataComponent = null, ERTCallComponent? component = null)
    {
        if (stationUid == null)
            return null;

        if (!Resolve(stationUid.Value, ref component, ref dataComponent))
        {
            return null;
        }

        if (!component.ERTCalled)
        {
            return null;
        }

        if (component.ApproveCooldownRemaining >= 0f && !component.AwaitsSpawn)
            return _gameTiming.CurTime + TimeSpan.FromSeconds(component.ApproveCooldownRemaining);

        if (component.SpawnCooldownRemaining >= 0f && component.AwaitsSpawn)
            return _gameTiming.CurTime + TimeSpan.FromSeconds(component.SpawnCooldownRemaining);

        return null;
    }

    private bool TryAddShuttle(ResPath shuttlePath, [NotNullWhen(true)] out EntityUid? shuttleGrid)
    {
        shuttleGrid = null;
        var shuttleMap = _map.CreateMap(out var shuttleMapId);

        if (!_loader.TryLoadGrid(shuttleMapId, shuttlePath, out var grid))
        {
            _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
            return false;
        }

        shuttleGrid = grid;
        return true;
    }

    public void Accept(ERTCallComponent component, string admin)
    {
        if (component.ERTCalledTeam == null)
            return;

        component.WasApproved = true;

        _chatManager.SendAdminAlert(Loc.GetString("ert-command-admin-alert-accept", ("admin", admin), ("ert", Loc.GetString(component.ERTCalledTeam.Name))));
    }

    public void FakeAccept(ERTCallComponent component, string admin)
    {
        if (component.ERTCalledTeam == null)
            return;

        _chatManager.SendAdminAlert(Loc.GetString("ert-command-admin-alert-fake-accept", ("admin", admin), ("ert", Loc.GetString(component.ERTCalledTeam.Name))));

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-accept-announcement-fake", ("name", Loc.GetString(component.ERTCalledTeam.Name))),
            playSound: true, colorOverride: Color.Gold, announcementSound: new SoundPathSpecifier(DecisionAnnouncementSound));

        component.ERTCalledTeam = null;
        component.ERTCalled = false;
        component.SpawnCooldownRemaining = 0;
        component.NewCallCooldownRemaining = Convert.ToSingle(component.TimeToAnotherSpawn.TotalSeconds);
        component.AwaitsSpawn = false;
    }

    public void Refuse(ERTCallComponent component, string admin)
    {
        if (component.ERTCalledTeam == null)
            return;

        _chatManager.SendAdminAlert(Loc.GetString("ert-command-admin-alert-refuse", ("admin", admin), ("ert", Loc.GetString(component.ERTCalledTeam.Name))));

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-refusal-announcement", ("name", Loc.GetString(component.ERTCalledTeam.Name))),
            playSound: true, colorOverride: Color.Gold, announcementSound: new SoundPathSpecifier(DecisionAnnouncementSound));

        component.ERTCalledTeam = null;
        component.ERTCalled = false;
        component.WasApproved = false;
        component.ApproveCooldownRemaining = 0;
        component.SpawnCooldownRemaining = 0;
    }

    public void AcceptMessage(ERTTeamDetail? ertTeamDetails, ERTCallComponent component)
    {
        if (ertTeamDetails == null)
            return;

        if (component.ERTTeams == null)
            return;

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-accept-announcement", ("name", Loc.GetString(ertTeamDetails.Name)),
            ("time", ertTeamDetails.TimeToSpawn.TotalMinutes)), playSound: true, colorOverride: Color.Gold, announcementSound: new SoundPathSpecifier(DecisionAnnouncementSound));

        component.ApproveCooldownRemaining = 0;
    }

    public void RefuseMessage(ERTTeamDetail? ertTeamDetails, ERTCallComponent component)
    {
        if (ertTeamDetails == null)
            return;

        if (component.ERTTeams == null)
            return;

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-refusal-announcement", ("name", Loc.GetString(ertTeamDetails.Name))),
            playSound: true, colorOverride: Color.Gold, announcementSound: new SoundPathSpecifier(DecisionAnnouncementSound));

        component.ERTCalledTeam = null;
        component.ERTCalled = false;
        component.WasApproved = false;
        component.ApproveCooldownRemaining = 0;
        component.SpawnCooldownRemaining = 0;
    }

    public void SpawnERT(ERTTeamDetail? ertTeamDetails, ERTCallComponent component)
    {
        if (ertTeamDetails == null)
            return;

        if (component.ERTTeams == null)
            return;

        if (!TryAddShuttle(ertTeamDetails.ShuttlePath, out var shuttleGrid))
            return;

        var spawns = new List<EntityCoordinates>();

        // Forgive me for hardcoding prototypes
        foreach (var (_, xform) in EntityManager.EntityQuery<SpawnPointComponent, TransformComponent>(true))
        {
            if (xform.ParentUid != shuttleGrid)
                continue;

            spawns.Add(xform.Coordinates);
            break;
        }

        if (spawns.Count == 0)
        {
            spawns.Add(EntityManager.GetComponent<TransformComponent>(shuttleGrid.Value).Coordinates);
        }

        foreach (var human in ertTeamDetails.HumansList)
        {
            for (var i = 0; i < human.Value; i++)
            {
                EntityManager.SpawnEntity(human.Key, _random.Pick(spawns));
            }
        }

        _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("ert-call-spawn-announcement", ("name", Loc.GetString(ertTeamDetails.Name))),
            playSound: true, colorOverride: Color.Gold, announcementSound: new SoundPathSpecifier(SpawnAnnouncementSound));

        component.ERTCalledTeam = null;
        component.ERTCalled = false;
        component.SpawnCooldownRemaining = 0;
        component.NewCallCooldownRemaining = Convert.ToSingle(component.TimeToAnotherSpawn.TotalSeconds);
        component.AwaitsSpawn = false;
    }

    public override void Update(float frameTime)
    {
        foreach (var comp in EntityQuery<ERTCallComponent>())
        {
            if (comp.NewCallCooldownRemaining > 0f)
            {
                comp.NewCallCooldownRemaining -= frameTime;
            }

            if (comp.ERTCalled)
            {
                if (comp.ApproveCooldownRemaining > 0f && !comp.AwaitsSpawn)
                {
                    comp.ApproveCooldownRemaining -= frameTime;
                }
                else if (comp.ApproveCooldownRemaining <= 0f && !comp.WasApproved && !comp.AwaitsSpawn)
                {
                    RefuseMessage(comp.ERTCalledTeam, comp);

                    comp.AwaitsSpawn = false;
                }
                else if (comp.ApproveCooldownRemaining <= 0f && comp.WasApproved && !comp.AwaitsSpawn)
                {
                    AcceptMessage(comp.ERTCalledTeam, comp);

                    comp.AwaitsSpawn = true;
                }

                if (comp.AwaitsSpawn)
                {
                    if (comp.SpawnCooldownRemaining > 0f)
                    {
                        comp.SpawnCooldownRemaining -= frameTime;
                    }
                    else if (comp.SpawnCooldownRemaining <= 0f)
                    {
                        SpawnERT(comp.ERTCalledTeam, comp);
                    }
                }
            }
        }
        base.Update(frameTime);
    }
}

public sealed class ERTCallEvent : EntityEventArgs
{
    public EntityUid Station { get; }

    public ERTCallEvent(EntityUid station)
    {
        Station = station;
    }
}

public sealed class ERTRecallEvent : EntityEventArgs
{
    public EntityUid Station { get; }

    public ERTRecallEvent(EntityUid station)
    {
        Station = station;
    }
}
