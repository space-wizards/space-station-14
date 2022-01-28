using System.Collections.Generic;
using System.Linq;
using Content.Shared.SubFloor;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.SubFloor;

public class TrayScannerSystem : SharedTrayScannerSystem
{
    [Dependency] private IEntityLookup _entityLookup = default!;
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
        _subfloorSystem.ToggleSubfloorEntities(scanner.RevealedSubfloors, false, uid, _visualizerKeys);
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
        if (!scanner.Toggled)
        {
            _subfloorSystem.ToggleSubfloorEntities(scanner.RevealedSubfloors, false, uid, _visualizerKeys);
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
            _subfloorSystem.ToggleSubfloorEntities(scanner.RevealedSubfloors, false, uid, _visualizerKeys);
            scanner.RevealedSubfloors.Clear();
            return true;
        }

        if (flooredPos == scanner.LastLocation
            || (float.IsNaN(flooredPos.X) && float.IsNaN(flooredPos.Y)))
            return true;

        scanner.LastLocation = flooredPos;

        // get all entities in range by uid
        // but without using LINQ
        HashSet<EntityUid> nearby = new();

        foreach (var entityInRange in _entityLookup.GetEntitiesInRange(uid, scanner.Range))
            if (FilterAnchored(entityInRange)) nearby.Add(entityInRange);

        // get all the old elements that are no longer detected
        scanner.RevealedSubfloors.ExceptWith(nearby);

        // hide all of them, since they're no longer needed
        _subfloorSystem.ToggleSubfloorEntities(scanner.RevealedSubfloors, false, uid, _visualizerKeys);
        scanner.RevealedSubfloors.Clear();

        // set the revealedsubfloor set to the new nearby set
        scanner.RevealedSubfloors.UnionWith(nearby);

        // show all the new subfloor
        _subfloorSystem.ToggleSubfloorEntities(scanner.RevealedSubfloors, true, uid, _visualizerKeys);

        return true;
    }

    private static IEnumerable<object> _visualizerKeys = new List<object>
    {
        SubFloorVisuals.SubFloor,
        TrayScannerTransparency.Key
    };

    private bool FilterAnchored(EntityUid uid)
    {
        return EntityManager.TryGetComponent<TransformComponent>(uid, out var transform)
            && transform.Anchored;
    }
}
