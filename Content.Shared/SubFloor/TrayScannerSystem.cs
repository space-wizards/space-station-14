using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.SubFloor;

public sealed class TrayScannerSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSubFloorHideSystem _subfloorSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private HashSet<EntityUid> _activeScanners = new();
    private RemQueue<EntityUid> _invalidScanners = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScannerComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TrayScannerComponent, ComponentGetState>(OnTrayScannerGetState);
        SubscribeLocalEvent<TrayScannerComponent, ComponentHandleState>(OnTrayScannerHandleState);
        SubscribeLocalEvent<TrayScannerComponent, ActivateInWorldEvent>(OnTrayScannerActivate);
    }

    private void OnTrayScannerActivate(EntityUid uid, TrayScannerComponent scanner, ActivateInWorldEvent args)
    {
        SetScannerEnabled(uid, !scanner.Enabled, scanner);
    }

    private void SetScannerEnabled(EntityUid uid, bool enabled, TrayScannerComponent? scanner = null)
    {
        if (!Resolve(uid, ref scanner))
            return;

        scanner.Enabled = enabled;
        Dirty(scanner);

        if (scanner.Enabled)
            _activeScanners.Add(uid);

        // We don't remove from _activeScanners on disabled, because the update function will handle that, as well as
        // managing the revealed subfloor entities

        if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, TrayScannerVisual.Visual, scanner.Enabled ? TrayScannerVisual.On : TrayScannerVisual.Off, appearance);
        }
    }

    private void OnTrayScannerGetState(EntityUid uid, TrayScannerComponent scanner, ref ComponentGetState args)
    {
        args.State = new TrayScannerState(scanner.Enabled);
    }

    private void OnTrayScannerHandleState(EntityUid uid, TrayScannerComponent scanner, ref ComponentHandleState args)
    {
        if (args.Current is not TrayScannerState state)
            return;

        SetScannerEnabled(uid, state.Enabled, scanner);

        // This is hacky and somewhat inefficient for the client. But when resetting predicted entities we have to unset
        // last position. This is because appearance data gets reset, but if the position isn't reset the scanner won't
        // re-reveal entities leading to odd visuals.
        scanner.LastLocation = null;
    }

    public void OnComponentShutdown(EntityUid uid, TrayScannerComponent scanner, ComponentShutdown args)
    {
        _subfloorSystem.SetEntitiesRevealed(scanner.RevealedSubfloors, uid, false);
        _activeScanners.Remove(uid);
    }

    public override void Update(float frameTime)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!_activeScanners.Any())
            return;

        foreach (var scanner in _activeScanners)
        {
            if (_invalidScanners.List != null
                && _invalidScanners.List.Contains(scanner))
                continue;

            if (!UpdateTrayScanner(scanner))
                _invalidScanners.Add(scanner);
        }

        foreach (var invalidScanner in _invalidScanners)
        {
            _activeScanners.Remove(invalidScanner);
        }

        _invalidScanners.List?.Clear();
    }

    /// <summary>
    ///     When a subfloor entity gets anchored (which includes spawning & coming into PVS range), Check for nearby scanners.
    /// </summary>
    public void OnSubfloorAnchored(EntityUid uid, SubFloorHideComponent? hideComp = null, TransformComponent? xform = null)
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
        if (!Resolve(uid, ref scanner, ref transform))
            return false;

        // if the scanner was toggled off recently,
        // set all the known subfloor to invisible,
        // and return false so it's removed from
        // the active scanner list
        if (!scanner.Enabled || transform.MapID == MapId.Nullspace)
        {
            _subfloorSystem.SetEntitiesRevealed(scanner.RevealedSubfloors, uid, false);
            scanner.LastLocation = null;
            scanner.RevealedSubfloors.Clear();
            return false;
        }

        var pos = transform.LocalPosition;
        var parent = _transform.GetParent(transform);

        // zero vector implies container
        //
        // this means we should get the entity transform's parent
        if (pos == Vector2.Zero
            && parent != null
            && _containerSystem.ContainsEntity(transform.ParentUid, uid))
        {
            pos = parent.LocalPosition;

            // if this is also zero, we can check one more time
            //
            // could recurse through fully but i think that's useless,
            // just attempt to check through the gp's transform and if
            // that doesn't work, just don't bother any further
            if (pos == Vector2.Zero)
            {
                var gpTransform = _transform.GetParent(parent);
                if (gpTransform != null
                    && _containerSystem.ContainsEntity(gpTransform.Owner, transform.ParentUid))
                {
                    pos = gpTransform.LocalPosition;
                }
            }
        }

        // is the position still logically zero? just clear,
        // but we need to keep it as 'true' since this t-ray
        // is still technically on
        if (pos == Vector2.Zero)
        {
            _subfloorSystem.SetEntitiesRevealed(scanner.RevealedSubfloors, uid, false);
            scanner.RevealedSubfloors.Clear();
            return true;
        }

        // get the rounded position so that small movements don't cause this to
        // update every time
        var flooredPos = (Vector2i) pos;

        // MAYBE redo this. Currently different players can see different entities
        //
        // Here we avoid the entity lookup & return early if the scanner's position hasn't appreciably changed. However,
        // if a new player enters PVS-range, they will update the in-range entities on their end and use that to set
        // LastLocation. This means that different players can technically see different entities being revealed by the
        // same scanner. The correct fix for this is probably just to network the revealed entity set.... But I CBF
        // doing that right now....
        if (flooredPos == scanner.LastLocation
            || float.IsNaN(flooredPos.X) && float.IsNaN(flooredPos.Y))
            return true;

        scanner.LastLocation = flooredPos;

        //  Update entities in Range
        HashSet<EntityUid> nearby = new();
        var coords = transform.MapPosition;
        var worldBox = Box2.CenteredAround(coords.Position, (scanner.Range * 2, scanner.Range * 2));

        // For now, limiting to the scanner's own grid. We could do a grid-lookup, but then what do we do if one grid
        // flies away, while the scanner's local-position remains unchanged?
        if (_mapManager.TryGetGrid(transform.GridUid, out var grid))
        {
            foreach (var entity in grid.GetAnchoredEntities(worldBox))
            {
                if (!Transform(entity).MapPosition.InRange(coords, scanner.Range))
                    continue;

                if (!TryComp(entity, out SubFloorHideComponent? hideComp))
                    continue; // Not a hide-able entity.

                nearby.Add(entity);

                if (scanner.RevealedSubfloors.Add(entity))
                    _subfloorSystem.SetEntityRevealed(entity, uid, true, hideComp);
            }
        }

        // get all the old elements that are no longer detected
        HashSet<EntityUid> missing = new(scanner.RevealedSubfloors.Except(nearby));

        // remove those from the list
        scanner.RevealedSubfloors.ExceptWith(missing);

        // and hide them
        _subfloorSystem.SetEntitiesRevealed(missing, uid, false);

        return true;
    }
}

[Serializable, NetSerializable]
public enum TrayScannerVisual : sbyte
{
    Visual,
    On,
    Off
}
