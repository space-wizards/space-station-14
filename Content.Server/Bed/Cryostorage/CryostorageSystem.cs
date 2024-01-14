using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Chat;
using Content.Shared.Climbing.Systems;
using Content.Shared.Mind.Components;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Bed.Cryostorage;

/// <inheritdoc/>
public sealed class CryostorageSystem : SharedCryostorageSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
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
        var comp = ent.Comp;

        if (!TryComp<CryostorageComponent>(comp.Cryostorage, out var cryostorageComponent))
            return;

        if (comp.GracePeriodEndTime != null)
            comp.GracePeriodEndTime = Timing.CurTime + cryostorageComponent.NoMindGracePeriod;
        comp.UserId = args.Mind.Comp.UserId;
    }

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.Session.AttachedEntity is not { } entity)
            return;

        if (!TryComp<CryostorageContainedComponent>(entity, out var containedComponent))
            return;

        if (args.NewStatus is SessionStatus.Disconnected or SessionStatus.Zombie)
        {
            if (CryoSleepRejoiningEnabled)
                containedComponent.StoredWhileDisconnected = true;

            var delay = CompOrNull<CryostorageComponent>(containedComponent.Cryostorage)?.NoMindGracePeriod ?? TimeSpan.Zero;
            containedComponent.GracePeriodEndTime = Timing.CurTime + delay;
            containedComponent.UserId = args.Session.UserId;
        }
        else if (args.NewStatus == SessionStatus.InGame)
        {
            HandleCryostorageReconnection((entity, containedComponent));
        }
    }

    public void HandleEnterCryostorage(Entity<CryostorageContainedComponent> ent, NetUserId userId)
    {
        var comp = ent.Comp;
        TryComp<CryostorageComponent>(ent.Comp.Cryostorage, out var cryostorageComponent);

        // if we have a session, we use that to add back in all the job slots the player had.
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
        _audio.PlayPvs(cryostorageComponent?.RemoveSound, ent);

        EnsurePausedMap();
        if (PausedMap == null)
        {
            Log.Error("CryoSleep map was unexpectedly null");
            return;
        }

        if (!comp.StoredWhileDisconnected && Mind.TryGetMind(userId, out var mind))
        {
            _gameTicker.OnGhostAttempt(mind.Value, false);
        }
        _transform.SetParent(ent, PausedMap.Value);
    }

    private void HandleCryostorageReconnection(Entity<CryostorageContainedComponent> entity)
    {
        var (uid, comp) = entity;
        if (!CryoSleepRejoiningEnabled || !comp.StoredWhileDisconnected)
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
        comp.StoredWhileDisconnected = false;
    }

    protected override void OnInsertedContainer(Entity<CryostorageComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var (uid, comp) = ent;
        if (args.Container.ID != comp.ContainerId)
            return;

        base.OnInsertedContainer(ent, ref args);

        var locKey = CryoSleepRejoiningEnabled
            ? "cryostorage-insert-message-temp"
            : "cryostorage-insert-message-permanent";

        var msg = Loc.GetString(locKey, ("time", comp.GracePeriod.TotalMinutes));
        if (TryComp<ActorComponent>(args.Entity, out var actor))
            _chatManager.ChatMessageToOne(ChatChannel.Server, msg, msg, uid, false, actor.PlayerSession.Channel);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CryostorageContainedComponent>();
        while (query.MoveNext(out var uid, out var containedComp))
        {
            if (containedComp.GracePeriodEndTime == null || containedComp.StoredWhileDisconnected)
                continue;

            if (Timing.CurTime < containedComp.GracePeriodEndTime)
                continue;

            Mind.TryGetMind(uid, out _, out var mindComp);
            var id = containedComp.UserId ?? mindComp?.UserId;
            if (id == null)
            {
                Log.Error($"trying to enter entity ${ToPrettyString(uid)} into cryostorage with no valid mind.");
                continue;
            }

            HandleEnterCryostorage((uid, containedComp), id.Value);
        }
    }
}
