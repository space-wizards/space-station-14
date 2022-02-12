using System.Linq;
using Content.Shared.SubFloor;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.SubFloor;

public sealed class TrayScannerSystem : SharedTrayScannerSystem
{
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private SubFloorHideSystem _subfloorSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<TrayScannerComponent, ComponentShutdown>(OnComponentShutdown);
    }

    public void OnComponentShutdown(EntityUid uid, TrayScannerComponent scanner, ComponentShutdown args)
    {
        _subfloorSystem.SetEntitiesRevealed(scanner.RevealedSubfloors, uid, false, _visualizerKeys);
        _invalidScanners.Add(uid);
    }

    public override void ToggleTrayScanner(EntityUid uid, bool toggle, TrayScannerComponent? scanner = null)
    {
        if (!Resolve(uid, ref scanner))
            return;

        scanner.Toggled = toggle;
        UpdateTrayScanner(uid, scanner);

        if (toggle) _activeScanners.Add(uid);
    }

    private HashSet<EntityUid> _activeScanners = new();
    private RemQueue<EntityUid> _invalidScanners = new();

    public override void Update(float frameTime)
    {
        if (!_activeScanners.Any()) return;

        foreach (var scanner in _activeScanners)
        {
            if (_invalidScanners.List != null
                && _invalidScanners.List.Contains(scanner))
                continue;

            if (!UpdateTrayScanner(scanner))
                _invalidScanners.Add(scanner);
        }

        foreach (var invalidScanner in _invalidScanners)
            _activeScanners.Remove(invalidScanner);

        if (_invalidScanners.List != null) _invalidScanners.List.Clear();
    }

    /// <summary>
    ///     When a subfloor entity gets anchored (which includes spawning & coming into PVS range), Check for nearby scanners.
    /// </summary>
    public override void OnSubfloorAnchored(EntityUid uid, SubFloorHideComponent? hideComp = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref hideComp, ref xform))
            return;
        
        var pos = xform.MapPosition;

        foreach (var entity in _activeScanners)
        {
            if (!TryComp(entity, out TrayScannerComponent? scanner))
                continue;

            if (!Transform(entity).MapPosition.InRange(pos, scanner.Range))
                continue;

            hideComp.RevealedBy.Add(entity);
            scanner.RevealedSubfloors.Add(uid);
        }
    }

    /// <summary>
    ///     Updates a T-Ray scanner. Should be called on immediate
    ///     state change (turned on/off), or during the update
    ///     loop.
    /// </summary>
    /// <returns>true if the update was successful, false otherwise</returns>
    private bool UpdateTrayScanner(EntityUid uid, TrayScannerComponent? scanner = null, TransformComponent? transform = null)
    {
        // whoops?
        if (!Resolve(uid, ref scanner, ref transform))
        {
            return false;
        }

        // if the scanner was toggled off recently,
        // set all the known subfloor to invisible,
        // and return false so it's removed from
        // the active scanner list
        if (!scanner.Toggled || transform.MapID == MapId.Nullspace)
        {
            _subfloorSystem.SetEntitiesRevealed(scanner.RevealedSubfloors, uid, false, _visualizerKeys);
            scanner.LastLocation = Vector2.Zero;
            scanner.RevealedSubfloors.Clear();
            return false;
        }

        // get the rounded position so that small movements don't cause this to
        // update every time
        Vector2 flooredPos;

        // zero vector implies container
        //
        // this means we should get the entity transform's parent
        if (transform.LocalPosition == Vector2.Zero
            && transform.Parent != null
            && _containerSystem.ContainsEntity(transform.ParentUid, uid))
        {
            flooredPos = transform.Parent.LocalPosition.Rounded();

            // if this is also zero, we can check one more time
            //
            // could recurse through fully but i think that's useless,
            // just attempt to check through the gp's transform and if
            // that doesn't work, just don't bother any further
            if (flooredPos == Vector2.Zero)
            {
                var gpTransform = transform.Parent.Parent;
                if (gpTransform != null
                    && _containerSystem.ContainsEntity(gpTransform.Owner, transform.ParentUid))
                {
                    flooredPos = gpTransform.LocalPosition.Rounded();
                }
            }
        }
        else
        {
            flooredPos = transform.LocalPosition.Rounded();
        }

        // is the position still logically zero? just clear,
        // but we need to keep it as 'true' since this t-ray
        // is still technically on
        if (flooredPos == Vector2.Zero)
        {
            _subfloorSystem.SetEntitiesRevealed(scanner.RevealedSubfloors, uid, false, _visualizerKeys);
            scanner.RevealedSubfloors.Clear();
            return true;
        }

        // MAYBE redo this. Currently different players can see different entities
        //
        // Here we avoid the entity lookup & return early if the scanner's position hasn't appreciably changed. However,
        // if a new player enters PVS-range, they will update the in-range entities on their end and use that to set
        // LastLocation. This means that different players can technically see different entities being revealed by the
        // same scanner. The correct fix for this is probably just to network the revealed entity set.... But I CBF
        // doing that right now....
        if (flooredPos == scanner.LastLocation
            || (float.IsNaN(flooredPos.X) && float.IsNaN(flooredPos.Y)))
            return true;

        scanner.LastLocation = flooredPos;

        //  Update entities in Range
        HashSet<EntityUid> nearby = new();
        var coords = transform.MapPosition;
        var worldBox = Box2.CenteredAround(coords.Position, (scanner.Range * 2, scanner.Range * 2));

        // For now, limiting to the scanner's own grid. We could do a grid-lookup, but then what do we do if one grid
        // flies away, while the scanner's local-position remains unchanged?
        if (_mapManager.TryGetGrid(transform.GridID, out var grid))
        {
            foreach (var entity in grid.GetAnchoredEntities(worldBox))
            {
                if (!Transform(entity).MapPosition.InRange(coords, scanner.Range))
                    continue;

                if (!TryComp(entity, out SubFloorHideComponent? hideComp))
                    continue; // Not a hide-able entity.

                nearby.Add(entity);

                if (scanner.RevealedSubfloors.Add(entity))
                    _subfloorSystem.SetEntityRevealed(entity, uid, true, hideComp, _visualizerKeys);
            }
        }

        // get all the old elements that are no longer detected
        HashSet<EntityUid> missing = new(scanner.RevealedSubfloors.Except(nearby));

        // remove those from the list
        scanner.RevealedSubfloors.ExceptWith(missing);

        // and hide them
        _subfloorSystem.SetEntitiesRevealed(missing, uid, false, _visualizerKeys);

        return true;
    }

    private static IEnumerable<object> _visualizerKeys = new List<object>
    {
        SubFloorVisuals.SubFloor,
        TrayScannerTransparency.Key
    };
}
