using System.Collections.Generic;
using System.Linq;
using Content.Shared.SubFloor;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Content.Client.SubFloor;

public class TrayScannerSystem : SharedTrayScannerSystem
{
    [Dependency] private IEntityLookup _entityLookup = default!;
    [Dependency] private SubFloorHideSystem _subfloorSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScannerComponent, TrayScannerToggleEvent>(OnTrayScannerToggle);
    }

    private void OnTrayScannerToggle(EntityUid uid, TrayScannerComponent scanner, TrayScannerToggleEvent args)
    {
        UpdateTrayScanner(uid, scanner);

        if (scanner.Toggled) _activeScanners.Add(uid);
    }

    private HashSet<EntityUid> _activeScanners = new();
    private HashSet<EntityUid> _invalidScanners = new();

    public override void Update(float frameTime)
    {
        if (!_activeScanners.Any()) return;

        foreach (var scanner in _activeScanners)
        {
            if (!UpdateTrayScanner(scanner))
                _invalidScanners.Add(scanner);
        }

        _activeScanners.ExceptWith(_invalidScanners);
        _invalidScanners.Clear();
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
        if (transform.LocalPosition == Vector2.Zero && transform.Parent != null)
            if (_containerSystem.ContainsEntity(transform.Parent.Owner.Uid, uid))
                flooredPos = transform.Parent.LocalPosition.Rounded();
            else
                flooredPos = transform.LocalPosition.Rounded();
        else
            flooredPos = transform.LocalPosition.Rounded();

        if (flooredPos == scanner.LastLocation
            || (float.IsNaN(flooredPos.X) && float.IsNaN(flooredPos.Y)))
            return true;

        scanner.LastLocation = flooredPos;
        // Logger.DebugS("TrayScannerSystem", $"newPos: {flooredPos}");

        if (!EntityManager.TryGetEntity(uid, out var entity))
            return true;

        // hide all the existing subfloor

        // get all entities in range by uid
        var nearby = _entityLookup.GetEntitiesInRange(entity, scanner.Range)
            .Select(ent => ent.Uid)
            .Where(FilterAnchored)
            .ToHashSet();

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
