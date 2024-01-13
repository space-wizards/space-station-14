using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Climbing.Systems;
using Content.Shared.Mind.Components;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Bed.Cryostorage;

/// <inheritdoc/>
public sealed class CryostorageSystem : SharedCryostorageSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryostorageContainedComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<CryostorageContainedComponent, MindRemovedMessage>(OnMindRemoved);

        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
    }

    private void OnPlayerSpawned(Entity<CryostorageContainedComponent> ent, ref PlayerSpawnCompleteEvent args)
    {
        // if you spawned into cryostorage, we're not gonna round-remove you.
        ent.Comp.GracePeriodEndTime = null;
    }

    private void OnMindRemoved(Entity<CryostorageContainedComponent> ent, ref MindRemovedMessage args)
    {
        HandleEnterCryostorage(ent, args.Mind.Comp.UserId);
    }

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.Session.AttachedEntity is not { } entity)
            return;

        Log.Debug($"status: {args.NewStatus}");
        if (!TryComp<CryostorageContainedComponent>(entity, out var containedComponent))
            return;
        Log.Debug($"great filter???");

        if (args.NewStatus is SessionStatus.Disconnected or SessionStatus.Zombie)
        {
            if (CryoSleepRejoiningEnabled)
                containedComponent.StoredOnMap = true;

            HandleEnterCryostorage((entity, containedComponent), args.Session.UserId);
        }
        else if (args.NewStatus == SessionStatus.InGame)
        {
            HandleCryostorageReconnection((entity, containedComponent));
        }
    }

    public void HandleEnterCryostorage(Entity<CryostorageContainedComponent> ent, NetUserId? netUserId)
    {
        if (!CryoSleepRejoiningEnabled || !ent.Comp.StoredOnMap)
        {
            // if we have a session, we use that to add back in all the job slots the player had.
            if (netUserId is { } userId)
            {
                foreach (var station in _station.GetStationsSet())
                {
                    if (!TryComp<StationJobsComponent>(station, out var stationJobs))
                        continue;

                    if (!_stationJobs.TryGetPlayerJobs(station, userId, out var jobs, stationJobs))
                        continue;

                    foreach (var job in jobs)
                    {
                        _stationJobs.TryAdjustJobSlot(station, job, 1, clamp: true);
                    }

                    _stationJobs.TryRemovePlayerJobs(station, userId, stationJobs);
                }
            }

            QueueDel(ent);
        }
        else
        {
            if (PausedMap == null)
            {
                Log.Error("CryoSleep map was unexpectedly null");
                return;
            }
            _transform.SetParent(ent, PausedMap.Value);
        }
    }

    private void HandleCryostorageReconnection(Entity<CryostorageContainedComponent> entity)
    {
        var (uid, comp) = entity;
        if (!CryoSleepRejoiningEnabled || !comp.StoredOnMap)
            return;

        // how did you destroy these? they're indestructible.
        if (TerminatingOrDeleted(comp.Cryostorage) || !TryComp<CryostorageComponent>(comp.Cryostorage, out var cryostorageComponent))
        {
            QueueDel(entity);
            return;
        }

        var cryoXform = Transform(comp.Cryostorage);
        _transform.SetParent(uid, cryoXform.ParentUid);
        _transform.SetCoordinates(uid, cryoXform.Coordinates);
        if (!_container.TryGetContainer(comp.Cryostorage, cryostorageComponent.ContainerId, out var container) ||
            !_container.Insert(uid, container, cryoXform))
        {
            _climb.ForciblySetClimbing(uid, comp.Cryostorage);
        }

        comp.GracePeriodEndTime = null;
        comp.StoredOnMap = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CryostorageContainedComponent>();
        while (query.MoveNext(out var uid, out var containedComp))
        {
            if (containedComp.GracePeriodEndTime == null || containedComp.StoredOnMap)
                continue;

            if (Timing.CurTime < containedComp.GracePeriodEndTime)
                continue;

            Mind.TryGetMind(uid, out _, out var mindComp);
            HandleEnterCryostorage((uid, containedComp), mindComp?.UserId);
        }
    }
}
