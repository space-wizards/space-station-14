using Content.Server.Fluids.Components;
using Content.Shared.Fluids;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

public sealed class PuddleDebugDebugOverlaySystem : SharedPuddleDebugOverlaySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    private readonly HashSet<IPlayerSession> _playerObservers = new();


    public bool ToggleObserver(IPlayerSession observer)
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

    private void RemoveObserver(IPlayerSession observer)
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


            foreach (var grid in _mapManager.FindGridsIntersecting(transform.MapID, worldBounds))
            {
                var data = new List<PuddleDebugOverlayData>();
                var gridUid = grid.Owner;

                if (!Exists(gridUid))
                    continue;

                foreach (var uid in grid.GetAnchoredEntities(worldBounds))
                {
                    PuddleComponent? puddle = null;
                    TransformComponent? xform = null;
                    if (!Resolve(uid, ref puddle, ref xform, false))
                        continue;

                    var pos = xform.Coordinates.ToVector2i(EntityManager, _mapManager);
                    var vol = _puddle.CurrentVolume(uid, puddle);
                    data.Add(new PuddleDebugOverlayData(pos, vol));
                }

                RaiseNetworkEvent(new PuddleOverlayDebugMessage(gridUid, data.ToArray()));
            }
        }

        NextTick = _timing.CurTime + Cooldown;
    }
}
