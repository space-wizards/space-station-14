using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Bed.Cryostorage;

/// <inheritdoc/>
public sealed class CryostorageSystem : SharedCryostorageSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryostorageContainedComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<CryostorageContainedComponent, MindRemovedMessage>(OnMindRemoved);

        _playerManager.PlayerStatusChanged += PlayerStatusChanged;
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

        if (!TryComp<CryostorageContainedComponent>(entity, out var containedComponent))
            return;

        if (args.NewStatus is SessionStatus.Disconnected or SessionStatus.Zombie)
        {
            HandleEnterCryostorage((entity, containedComponent), args.Session.UserId);
        }
    }

    public void HandleEnterCryostorage(Entity<CryostorageContainedComponent> ent, NetUserId? netUserId)
    {
        if (CryostorageMapEnabled)
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

        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CryostorageContainedComponent>();
        while (query.MoveNext(out var uid, out var containedComp))
        {
            if (containedComp.GracePeriodEndTime == null)
                continue;

            if (Timing.CurTime < containedComp.GracePeriodEndTime)
                continue;

            Mind.TryGetMind(uid, out _, out var mindComp);
            HandleEnterCryostorage((uid, containedComp), mindComp?.UserId);
        }
    }
}
