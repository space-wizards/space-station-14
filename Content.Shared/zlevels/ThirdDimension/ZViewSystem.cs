using System.Linq;
using Content.Shared.Body.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Content.Shared._Afterlight.ThirdDimension;

/// <summary>
/// This handles view between z levels
/// </summary>
public abstract class SharedZViewSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly SharedZLevelSystem _zLevel = default!;

    private const int ViewDepth = 3;

    public override void Initialize()
    {
        SubscribeLocalEvent<ZViewComponent, ComponentHandleState>(ZViewComponentHandleState);
        SubscribeLocalEvent<ZViewComponent, ComponentGetState>(ZViewComponentGetState);

    }

    private void ZViewComponentGetState(EntityUid uid, ZViewComponent component, ref ComponentGetState args)
    {
        args.State = new ZViewComponentState(component.DownViewEnts);
    }

    private void ZViewComponentHandleState(EntityUid uid, ZViewComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ZViewComponentState state)
            return;

        component.DownViewEnts = state.DownViewEnts;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsServer)
            FrameUpdate(frameTime);
    }

    /// <inheritdoc/>
    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<SharedEyeComponent>();
        var toUpdate = new List<EntityUid>();
        while (query.MoveNext(out var uid, out _))
        {
            var view = EnsureComp<ZViewComponent>(uid);
            var xform = Transform(uid);
            var maps = new MapId[ViewDepth];
            var amt = _zLevel.AllMapsBelow(xform.MapID, ref maps);
            if (amt == 0)
                continue;

            var currPos = _xformSystem.GetWorldPosition(xform);

            if (view.DownViewEnts.Count != amt)
            {
                if (_net.IsClient || !CanSetup(uid))
                    continue;
                toUpdate.Add(uid);
                Logger.Debug("Queued Z view update.");
                continue;
            }

            foreach (var (ent, map) in view.DownViewEnts.Zip(maps))
            {
                if (map == MapId.Nullspace)
                    continue;

                var coords = EntityCoordinates.FromMap(_map, new MapCoordinates(currPos, map));
                _xformSystem.SetCoordinates(ent, coords);
            }
        }

        foreach (var uid in toUpdate)
        {
            Logger.Debug("Did z view update.");
            var view = EnsureComp<ZViewComponent>(uid);
            var xform = Transform(uid);
            foreach (var e in view.DownViewEnts)
            {
                QueueDel(e);
            }
            view.DownViewEnts.Clear();
            var maps = new MapId[ViewDepth];
            var amt = _zLevel.AllMapsBelow(xform.MapID, ref maps);
            if (amt == 0)
                continue;
            var currPos = _xformSystem.GetWorldPosition(xform);
            foreach (var map in maps)
            {
                if (map == MapId.Nullspace)
                    continue;
                view.DownViewEnts.Add(SpawnViewEnt(uid, new MapCoordinates(currPos, map)));
            }

            Dirty(view);
        }
    }

    public abstract EntityUid SpawnViewEnt(EntityUid source, MapCoordinates loc);
    public abstract bool CanSetup(EntityUid source);

}
