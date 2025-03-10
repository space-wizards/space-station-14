using System.Numerics;
using System.Threading;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.CosmicCult.Visuals;

/// <summary>
/// This handles rendering a preview of where the monument will be placed
/// </summary>
public sealed class MonumentPlacementPreviewSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private MonumentPlacementPreviewOverlay? _cachedOverlay = null;
    private CancellationTokenSource? _cancellationTokenSource = null;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonumentPlacementMarkerComponent, ActionAttemptEvent>(OnAttemptMonumentPlacement);
        //commented out because idrk if it's nece
        SubscribeLocalEvent<CosmicCultLeadComponent, EventCosmicPlaceMonument>(OnCosmicPlaceMonument);
    }

    //todo this needs to check for validity before starting the timer?
    //could also just remove the early overlay killing? it's technically not needed as it'll get 100% covered by the monument so ???
    private void OnCosmicPlaceMonument(Entity<CosmicCultLeadComponent> ent, ref EventCosmicPlaceMonument args)
    {
        //_overlay.RemoveOverlay<MonumentPlacementPreviewOverlay>();
        if (_cachedOverlay == null || _cancellationTokenSource == null)
            return;

        if (!VerifyPlacement(Transform(args.Performer)))
            return;

        _cachedOverlay.LockPlacement = true;
        _cancellationTokenSource.Cancel(); //cancel the previous timer

        //remove the overlay automatically after the primeTime expires
        //no cancellation token for this one as this'll never need to get cancelled afaik
        Robust.Shared.Timing.Timer.Spawn(TimeSpan.FromSeconds(3.8), //anim takes 3.8s, might want to have the ghost disappear earlier but eh
            () =>
            {
                _overlay.RemoveOverlay<MonumentPlacementPreviewOverlay>();
                _cachedOverlay = null;
                _cancellationTokenSource = null;
            }
        );
    }

    //duplicate code bad but also I cba to extract it from the overlay & make this work for both of them
    public bool VerifyPlacement(TransformComponent xform)
    {
        var spaceDistance = 3;
        var worldPos = _transformSystem.GetWorldPosition(xform);
        var pos = xform.LocalPosition + new Vector2(0, 1f);
        var box = new Box2(pos + new Vector2(-1.4f, -0.4f), pos + new Vector2(1.4f, 0.4f));

        // MAKE SURE WE'RE STANDING ON A GRID
        if (!_entityManager.TryGetComponent<MapGridComponent>(xform.GridUid, out var grid))
        {
            return false;
        }

        // CHECK IF IT'S BEING PLACED CHEESILY CLOSE TO SPACE
        foreach (var tile in _mapSystem.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, spaceDistance)))
        {
            if (!tile.IsSpace(_tileDef))
                continue;
            return false;
        }

        // cannot do this check clientside todo fix this? not sure if that's even possible
        // CHECK IF WE'RE ON THE STATION OR IF SOMEONE'S TRYING TO SNEAK THIS ONTO SOMETHING SMOL
        //var station = _station.GetStationInMap(xform.MapID);
        //EntityUid? stationGrid = null;

        //if (!_entityManager.TryGetComponent<StationDataComponent>(station, out var stationData))
        //    stationGrid = _station.GetLargestGrid(stationData);

        //if (stationGrid is not null && stationGrid != xform.GridUid)
        //{
        //    return false;
        //}

        // CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS
        if (_lookup.AnyLocalEntitiesIntersecting(xform.GridUid.Value, box, LookupFlags.Dynamic | LookupFlags.Static, _playerManager.LocalEntity))
        {
            return false;
        }

        //if all of those aren't false, return true
        return true;
    }

    private void OnAttemptMonumentPlacement(Entity<MonumentPlacementMarkerComponent> ent, ref ActionAttemptEvent args)
    {
        if (!TryComp<ConfirmableActionComponent>(ent, out var confirmableActionComponent))
            return; //return if the action somehow doesn't have a confirmableAction comp

        if (_cachedOverlay != null) //if we've already got a cached overlay, just return early
            return;

        _cancellationTokenSource = new CancellationTokenSource();
        _cachedOverlay = new MonumentPlacementPreviewOverlay(_entityManager, _playerManager, _spriteSystem, _mapSystem, _protoMan, this); //it's probably inefficient to make a new one every time, but this'll be happening like four times a round maybe
        _overlay.AddOverlay(_cachedOverlay);

        //remove the overlay automatically after the primeTime expires
        Robust.Shared.Timing.Timer.Spawn(confirmableActionComponent.PrimeTime + confirmableActionComponent.ConfirmDelay,
            () =>
            {
                _overlay.RemoveOverlay<MonumentPlacementPreviewOverlay>();
                _cachedOverlay = null;
            },
            _cancellationTokenSource.Token
        );
    }
}
