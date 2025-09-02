using System.Linq;
using Content.Server.Bed.Cryostorage;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._Starlight.Polymorph.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.GameTicking;
using Content.Shared.Starlight.CryoTeleportation;
using Content.Shared.Starlight.CCVar;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Content.Shared.Station.Components;

namespace Content.Server.Starlight.CryoTeleportation;

public sealed class CryoTeleportationSystem : EntitySystem
{
    [Dependency] private readonly CryostorageSystem _cryostorage = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    
    public TimeSpan NextTick = TimeSpan.Zero;
    public TimeSpan RefreshCooldown = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnCompleteSpawn);
        SubscribeLocalEvent<TargetCryoTeleportationComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<TargetCryoTeleportationComponent, PlayerAttachedEvent>(OnPlayerAttached);
        _playerMan.PlayerStatusChanged += OnSessionStatus;
    }

    public override void Update(float delay)
    {
        if (NextTick > _timing.CurTime)
            return;
        
        NextTick += RefreshCooldown;
        
        var query = AllEntityQuery<TargetCryoTeleportationComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var comp, out var mobStateComponent))
        {
            if (comp.Station == null 
                || !TryComp<StationCryoTeleportationComponent>(comp.Station, out var stationComp)
                || !TryComp<StationDataComponent>(comp.Station, out var stationData)
                || HasComp<BorgChassisComponent>(uid)
                || mobStateComponent.CurrentState != MobState.Alive
                || comp.ExitTime == null
                || _timing.CurTime - comp.ExitTime < stationComp.TransferDelay 
                || HasComp<CryostorageContainedComponent>(uid)
                || HasComp<UncryoableComponent>(uid))
                continue;

            var stationGrid = _stationSystem.GetLargestGrid((comp.Station.Value, stationData));

            if (stationGrid == null)
                continue;
            
            var cryoStorage = FindCryoStorage(Transform(stationGrid.Value));
            
            if (cryoStorage == null)
                continue;

            var containedComp = AddComp<CryostorageContainedComponent>(uid);
            
            containedComp.Cryostorage = cryoStorage.Value;
            containedComp.GracePeriodEndTime = _timing.CurTime;
            
            var portalCoordinates = _transformSystem.GetMapCoordinates(Transform(uid));

            var portalUid = _entity.SpawnEntity(stationComp.PortalPrototype, portalCoordinates);
            _audio.PlayPvs(stationComp.TransferSound, portalUid);
            
            var container = _container.EnsureContainer<ContainerSlot>(cryoStorage.Value, "storage");
            
            if (!_container.Insert(uid, container))
                _cryostorage.HandleEnterCryostorage((uid, containedComp), comp.UserId);
        }
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
        targetComponent.UserId = ev.Player.UserId;
    }

    private void OnPlayerDetached(EntityUid uid, TargetCryoTeleportationComponent comp, PlayerDetachedEvent ev)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobStateComponent) 
            || mobStateComponent.CurrentState != MobState.Alive)
            return;
        if (comp.ExitTime == null)
            comp.ExitTime = _timing.CurTime;
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            comp.UserId = mind.UserId;
    }
    
    private void OnPlayerAttached(EntityUid uid, TargetCryoTeleportationComponent comp, PlayerAttachedEvent ev)
    {
        if (comp.ExitTime != null)
            comp.ExitTime = null;
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            comp.UserId = mind.UserId;
    }
    
    private void OnSessionStatus(object? sender, SessionStatusEventArgs args)
    {
        if (!TryComp<TargetCryoTeleportationComponent>(args.Session.AttachedEntity, out var comp))
            return;

        if (args.Session.Status == SessionStatus.Disconnected && comp.ExitTime == null)
            comp.ExitTime = _timing.CurTime;
        else if (args.Session.Status == SessionStatus.Connected && comp.ExitTime != null)
            comp.ExitTime = null;

        comp.UserId = args.Session.UserId;
    }

    private EntityUid? FindCryoStorage(TransformComponent stationGridTransform)
    {
        var query = AllEntityQuery<CryostorageComponent, TransformComponent>();
        while (query.MoveNext(out var cryoUid, out _, out var cryoTransform))
        {
            if (stationGridTransform.MapUid != cryoTransform.MapUid)
                continue;
            
            var container = _container.EnsureContainer<ContainerSlot>(cryoUid, "storage");
            
            if (container.ContainedEntities.Count > 0)
                continue;

            return cryoUid;
        }

        return null;
    }
}