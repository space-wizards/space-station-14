using System.Threading;
using System.Threading.Tasks;
using Content.Shared.ActionBlocker;
using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Backmen.Blob;

public sealed class BlobObserverMover : Job<object>
{
    public BlobObserverMover(EntityManager entityManager, ActionBlockerSystem blockerSystem, SharedTransformSystem transform, BlobObserverSystem observerSystem, double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _observerSystem = observerSystem;
        _transform = transform;
        _blocker = blockerSystem;
        _entityManager = entityManager;
    }

    public BlobObserverMover(EntityManager entityManager, ActionBlockerSystem blockerSystem, SharedTransformSystem transform, BlobObserverSystem observerSystem, double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
    {
        _observerSystem = observerSystem;
        _transform = transform;
        _blocker = blockerSystem;
        _entityManager = entityManager;
    }
    public EntityCoordinates NewPosition;
    public Entity<BlobObserverComponent> Observer;

    private BlobObserverSystem _observerSystem;
    private SharedTransformSystem _transform;
    private ActionBlockerSystem _blocker;
    private EntityManager _entityManager;


    protected override async Task<object?> Process()
    {
        try
        {
            if (Observer.Comp.Core == null)
            {
                return default;
            }

            if (_entityManager.Deleted(Observer.Comp.Core.Value) ||
                !_entityManager.TryGetComponent<TransformComponent>(Observer.Comp.Core.Value, out var xform))
            {
                return default;
            }

            var corePos = xform.Coordinates;

            var (nearestEntityUid, nearestDistance) = _observerSystem.CalculateNearestBlobTileDistance(NewPosition);

            if (nearestEntityUid == null)
                return default;

            if (nearestDistance > 5f)
            {
                _transform.SetCoordinates(Observer, corePos);
                return default;
            }

            if (nearestDistance > 3f)
            {
                Observer.Comp.CanMove = true;
                _blocker.UpdateCanMove(Observer);
                var direction = (_entityManager.GetComponent<TransformComponent>(nearestEntityUid.Value).Coordinates.Position - NewPosition.Position);
                var newPosition = NewPosition.Offset(direction * 0.1f);

                _transform.SetCoordinates(Observer, newPosition);
            }

            return default;
        }
        finally
        {
            Observer.Comp.IsProcessingMoveEvent = false;
        }
    }
}
