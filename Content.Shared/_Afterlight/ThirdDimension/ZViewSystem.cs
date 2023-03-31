using System.Linq;
using Content.Shared.Body.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared._Afterlight.ThirdDimension;

/// <summary>
/// This handles view between z levels
/// </summary>
public abstract class SharedZViewSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IMapManager _map = default!;
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
        while (query.MoveNext(out var uid, out var eye))
        {
            var view = EnsureComp<ZViewComponent>(uid);
            var xform = Transform(uid);
            var maps = new MapId[ViewDepth];
            var amt = _zLevel.AllMapsBelow(xform.MapID, ref maps);
            if (amt == 0)
                continue;
            Array.Resize(ref maps, amt);

            var currPos = _xformSystem.GetWorldPosition(xform);

            if (view.DownViewEnts.Count != amt)
            {
                if (_net.IsClient || !CanSetup(uid))
                    continue;

                foreach (var e in view.DownViewEnts)
                {
                    Del(e);
                }

                view.DownViewEnts.Clear();

                foreach (var map in maps.Reverse())
                {
                    view.DownViewEnts.Add(SpawnViewEnt(uid, eye, new MapCoordinates(currPos, map)));
                    Dirty(view);
                }
            }

            foreach (var (ent, map) in view.DownViewEnts.Zip(maps))
            {
                var coords = EntityCoordinates.FromMap(_map, new MapCoordinates(currPos, map));
                _xformSystem.SetCoordinates(ent, coords);
            }
        }
    }

    public abstract EntityUid SpawnViewEnt(EntityUid source, SharedEyeComponent eye, MapCoordinates loc);
    public abstract bool CanSetup(EntityUid source);

}
