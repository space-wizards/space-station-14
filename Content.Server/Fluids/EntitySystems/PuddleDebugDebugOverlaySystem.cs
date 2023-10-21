using System.Numerics;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

public sealed class PuddleDebugDebugOverlaySystem : SharedPuddleDebugOverlaySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    private readonly HashSet<ICommonSession> _playerObservers = new();
    private List<Entity<MapGridComponent>> _grids = new();

    public bool ToggleObserver(ICommonSession observer)
    {
        NextTick ??= _timing.CurTime + Cooldown;

        if (_playerObservers.Contains(observer))
        {
            RemoveObserver(observer);
            return false;
        }

        _playerObservers.Add(observer);
        return true;
    }

    private void RemoveObserver(ICommonSession observer)
    {
        if (!_playerObservers.Remove(observer))
        {
            return;
        }

        var message = new PuddleOverlayDisableMessage();
        RaiseNetworkEvent(message, observer.ConnectedClient);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (NextTick == null || _timing.CurTime < NextTick)
            return;

        foreach (var session in _playerObservers)
        {
            if (session.AttachedEntity is not { Valid: true } entity)
                continue;

            var transform = EntityManager.GetComponent<TransformComponent>(entity);

            var worldBounds = Box2.CenteredAround(transform.WorldPosition,
                new Vector2(LocalViewRange, LocalViewRange));

            _grids.Clear();
            _mapManager.FindGridsIntersecting(transform.MapID, worldBounds, ref _grids);

            foreach (var grid in _grids)
            {
                var data = new List<PuddleDebugOverlayData>();
                var gridUid = grid.Owner;

                if (!Exists(gridUid))
                    continue;

                foreach (var uid in grid.Comp.GetAnchoredEntities(worldBounds))
                {
                    PuddleComponent? puddle = null;
                    TransformComponent? xform = null;
                    if (!Resolve(uid, ref puddle, ref xform, false))
                        continue;

                    var pos = xform.Coordinates.ToVector2i(EntityManager, _mapManager);
                    var vol = _puddle.CurrentVolume(uid, puddle);
                    data.Add(new PuddleDebugOverlayData(pos, vol));
                }

                RaiseNetworkEvent(new PuddleOverlayDebugMessage(GetNetEntity(gridUid), data.ToArray()));
            }
        }

        NextTick = _timing.CurTime + Cooldown;
    }
}
