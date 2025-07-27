using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Mind;

public sealed class MindStatusSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private const float UpdateInterval = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindStatusComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MindContainerComponent, ComponentStartup>(OnContainerStartup);
        SubscribeLocalEvent<MindContainerComponent, ComponentAdd>(OnContainerAdded);
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ForceUpdateMindStatusEvent>(OnForceUpdate);

        if (_net.IsServer)
        {
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_net.IsServer)
        {
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        if (_timing.CurTime < _nextUpdate) // Periodic update for edge cases
            return;

        _nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(UpdateInterval);

        var query = EntityQueryEnumerator<MindStatusComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var status, out var container))
        {
            UpdateMindStatus(uid, status, container);
        }
    }

    private void OnStartup(EntityUid uid, MindStatusComponent component, ComponentStartup args)
    {
        if (_net.IsClient)
            return;

        if (TryComp<MindContainerComponent>(uid, out var container))
            UpdateMindStatus(uid, component, container);
    }

    private void OnMindAdded(EntityUid uid, MindContainerComponent component, MindAddedMessage args)
    {
        if (_net.IsClient)
            return;

        EnsureComp<MindStatusComponent>(uid);
        if (TryComp<MindStatusComponent>(uid, out var status))
            UpdateMindStatus(uid, status, component);
    }

    private void OnMindRemoved(EntityUid uid, MindContainerComponent component, MindRemovedMessage args)
    {
        if (_net.IsClient)
            return;

        if (TryComp<MindStatusComponent>(uid, out var status))
            UpdateMindStatus(uid, status, component);
    }

    private void OnContainerStartup(EntityUid uid, MindContainerComponent component, ComponentStartup args)
    {
        if (_net.IsClient)
            return;

        // Ensure all entities with MindContainerComponent also have MindStatusComponent
        var status = EnsureComp<MindStatusComponent>(uid);
        UpdateMindStatus(uid, status, component);
    }

    private void OnContainerAdded(EntityUid uid, MindContainerComponent component, ComponentAdd args)
    {
        if (_net.IsClient)
            return;

        var status = EnsureComp<MindStatusComponent>(uid);
        UpdateMindStatus(uid, status, component);
    }

    private void OnForceUpdate(ForceUpdateMindStatusEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<MindStatusComponent>(args.Entity, out var status))
            return;

        if (!TryComp<MindContainerComponent>(args.Entity, out var container))
            return;

        UpdateMindStatus(args.Entity, status, container);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        // When a player connects/disconnects, update all entities with their mind
        if (!_mindSystem.TryGetMind(args.Session.UserId, out var mindId, out var mind))
            return;

        if (mind.OwnedEntity != null && TryComp<MindStatusComponent>(mind.OwnedEntity.Value, out var status) && TryComp<MindContainerComponent>(mind.OwnedEntity.Value, out var container))
        {
            UpdateMindStatus(mind.OwnedEntity.Value, status, container);
        }

        // Also check visiting entity
        if (mind.VisitingEntity != null && TryComp<MindStatusComponent>(mind.VisitingEntity.Value, out var visitStatus) && TryComp<MindContainerComponent>(mind.VisitingEntity.Value, out var visitContainer))
        {
            UpdateMindStatus(mind.VisitingEntity.Value, visitStatus, visitContainer);
        }
    }

    private void UpdateMindStatus(EntityUid uid, MindStatusComponent status, MindContainerComponent container)
    {
        var oldStatus = status.Status;
        var dead = _mobState.IsDead(uid);
        var mind = container.Mind != null ? CompOrNull<MindComponent>(container.Mind) : null;
        var hasUserId = mind?.UserId;
        var hasActiveSession = hasUserId != null && _playerManager.ValidSessionId(hasUserId.Value);

        // Determine new status based on the scenarios
        if (dead && hasUserId == null)
            status.Status = MindStatus.DeadAndIrrecoverable;
        else if (dead && !hasActiveSession)
            status.Status = MindStatus.DeadAndSSD;
        else if (dead)
            status.Status = MindStatus.Dead;
        else if (hasUserId == null)
            status.Status = MindStatus.Catatonic;
        else if (!hasActiveSession)
            status.Status = MindStatus.SSD;
        else
            status.Status = MindStatus.Active;

        if (oldStatus != status.Status)
            Dirty(uid, status);
    }
}
