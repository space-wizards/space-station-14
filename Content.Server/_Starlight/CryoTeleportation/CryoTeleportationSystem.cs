using System.Linq;
using Content.Server.Bed.Cryostorage;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.GameTicking;
using Content.Shared.Starlight.CryoTeleportation;
using Content.Shared.Starlight.CCVar;
using Content.Shared.Mind;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;

namespace Content.Server.Starlight.CryoTeleportation;

public sealed class CryoTeleportationSystem : EntitySystem
{
    [Dependency] private readonly CryostorageSystem _cryostorage = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    
    public TimeSpan NextTick = TimeSpan.Zero;
    public TimeSpan RefreshCooldown = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnCompleteSpawn);
        _playerMan.PlayerStatusChanged += OnSessionStatus;
    }

    public override void Update(float delay)
    {
        if (NextTick > _timing.CurTime)
            return;
        
        NextTick += RefreshCooldown;
        
        var query = AllEntityQuery<TargetCryoTeleportationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Station == null 
                || !TryComp<StationCryoTeleportationComponent>(comp.Station, out var stationComp) 
                || comp.ExitTime == null
                || _timing.CurTime - comp.ExitTime < stationComp.TransferDelay
                || HasComp<CryostorageContainedComponent>(uid))
                continue;

            var containedComp = AddComp<CryostorageContainedComponent>(uid);

            containedComp.Cryostorage = FindCryo(comp.Station.Value, Transform(uid));
            containedComp.GracePeriodEndTime = _timing.CurTime;

            _mind.TryGetMind(uid, out var _, out var mindComponent);

            if (mindComponent == null)
                continue;

            _entity.SpawnEntity(stationComp.PortalPrototype, Transform(uid).Coordinates);
            _audio.PlayPvs(stationComp.TransferSound, Transform(uid).Coordinates);

            _cryostorage.HandleEnterCryostorage((uid, containedComp), mindComponent.UserId);
        }
    }

    private void OnStationInitialized(StationInitializedEvent ev)
    {
        if (FindCryo(ev.Station, Transform(ev.Station)) == null 
            || !_configurationManager.GetCVar(StarlightCCVars.CryoTeleportation))
            return;
        EnsureComp<StationCryoTeleportationComponent>(ev.Station);
    }

    private void OnCompleteSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (!HasComp<StationCryoTeleportationComponent>(ev.Station) 
            || ev.JobId == null
            || ev.Player.AttachedEntity == null 
            || !_configurationManager.GetCVar(StarlightCCVars.CryoTeleportation))
            return;

        var targetComponent = EnsureComp<TargetCryoTeleportationComponent>(ev.Player.AttachedEntity.Value);
        targetComponent.Station = ev.Station;
    }

    private void OnSessionStatus(object? sender, SessionStatusEventArgs args)
    {
        if (!TryComp<TargetCryoTeleportationComponent>(args.Session.AttachedEntity, out var comp))
            return;

        if (args.Session.Status == SessionStatus.Disconnected && comp.ExitTime == null)
        {
            comp.ExitTime = _timing.CurTime;
        }
        else if (args.Session.Status == SessionStatus.Connected)
        {
            comp.ExitTime = null;
        }

        comp.UserId = args.Session.UserId;
    }

    private EntityUid? FindCryo(EntityUid station, TransformComponent entityXform)
    {
        var query = AllEntityQuery<CryostorageComponent>();
        while (query.MoveNext(out var cryoUid, out var cryostorageComponent))
        {
            if (!TryComp<TransformComponent>(cryoUid, out var cryoTransform))
                return null;

            if (entityXform.MapUid != cryoTransform.MapUid)
                continue;

            return cryoUid;
        }

        return null;
    }
}