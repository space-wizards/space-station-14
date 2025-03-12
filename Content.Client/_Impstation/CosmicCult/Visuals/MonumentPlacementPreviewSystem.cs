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
using Robust.Shared.Timing;

namespace Content.Client._Impstation.CosmicCult.Visuals;

/// <summary>
/// This handles rendering a preview of where the monument will be placed
/// </summary>
public sealed class MonumentPlacementPreviewSystem : EntitySystem
{
    //most of these aren't used by this system, see MonumentPlacementPreviewOverlay for a note on why they're here
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private MonumentPlacementPreviewOverlay? _cachedOverlay = null;
    private CancellationTokenSource? _cancellationTokenSource = null;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _cancellationTokenSource = null; //reset these to make 100% sure that they're safe
        _cachedOverlay = null;

        SubscribeLocalEvent<MonumentPlacementMarkerComponent, ActionAttemptEvent>(OnAttemptMonumentPlacement);
        SubscribeLocalEvent<CosmicCultLeadComponent, EventCosmicPlaceMonument>(OnCosmicPlaceMonument);
        SubscribeLocalEvent<CosmicCultLeadComponent, EventCosmicMoveMonument>(OnCosmicMoveMonument);
    }

    private void OnCosmicMoveMonument(Entity<CosmicCultLeadComponent> ent, ref EventCosmicMoveMonument args)
    {
        if (_cachedOverlay == null || _cancellationTokenSource == null)
            return;

        if (!VerifyPlacement(Transform(args.Performer), out _))
            return;

        _cachedOverlay.LockPlacement = true;
        _cancellationTokenSource.Cancel(); //cancel the previous timer

        //remove the overlay automatically after the primeTime expires
        //no cancellation token for this one as this'll never need to get cancelled afaik
        Robust.Shared.Timing.Timer.Spawn(TimeSpan.FromSeconds(3.8), //anim takes 3.8s, might want to have the ghost disappear earlier but eh todo tune this to whatever anim I end up on for the move
            () =>
            {
                _overlay.RemoveOverlay<MonumentPlacementPreviewOverlay>();
                _cachedOverlay = null;
                _cancellationTokenSource = null; //technically doesn't need to be nulled out as it's not checked against but let's be sane about this
            }
        );
    }

    //reasoning about this in my head
    //from default state (both null)
    //attempt event fires
    //sets to both a real value
    //starts timer
    //after that
    //further failed attempts get caught by cachedOverlay not being null, another timer won't stack w/ an existing one
    //successful attempts also don't re-start the timer due to making cachedOverlay real
    //when the monumentPlaced event fires, the old timer gets cancelled an a new one appears in it's place
    //I'm like 90% sure that this works good but I need some of the chumps in the discord to bugtest this with hammers

    private void OnCosmicPlaceMonument(Entity<CosmicCultLeadComponent> ent, ref EventCosmicPlaceMonument args)
    {
        if (_cachedOverlay == null || _cancellationTokenSource == null)
            return;

        if (!VerifyPlacement(Transform(args.Performer), out _))
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
                _cancellationTokenSource = null; //technically doesn't need to be nulled out as it's not checked against but let's be sane about this
            }
        );
    }

    //duplicated from the ability check, minus the station check because that can't be done clientside afaik?
    public bool VerifyPlacement(TransformComponent xform, out EntityCoordinates outPos)
    {
        outPos = new EntityCoordinates();

        //MAKE SURE WE'RE STANDING ON A GRID
        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
        {
            return false;
        }

        //CHECK IF IT'S BEING PLACED CHEESILY CLOSE TO SPACE
        var spaceDistance = 3;
        var worldPos = _transform.GetWorldPosition(xform); //this is technically wrong but basically fine; if
        foreach (var tile in _map.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, spaceDistance)))
        {
            if (tile.IsSpace(_tileDef))
            {
                return false;
            }
        }

        var localTile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        var targetIndices = localTile.GridIndices + new Vector2i(0, 1);
        var pos = _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid);
        outPos = pos;
        var box = new Box2(pos.Position + new Vector2(-1.4f, -0.4f), pos.Position + new Vector2(1.4f, 0.4f));

        //CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS
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
        //it's probably inefficient to make a new one every time, but this'll be happening like four times a round maybe
        //massive ctor because iocmanager hates me
        _cachedOverlay = new MonumentPlacementPreviewOverlay(_entityManager, _playerManager, _spriteSystem, _map, _protoMan, this, _timing, ent.Comp.Tier);
        _overlay.AddOverlay(_cachedOverlay);

        //remove the overlay automatically after the primeTime expires
        Robust.Shared.Timing.Timer.Spawn(confirmableActionComponent.PrimeTime + confirmableActionComponent.ConfirmDelay,
            () =>
            {
                _overlay.RemoveOverlay<MonumentPlacementPreviewOverlay>();
                _cachedOverlay = null;
                _cancellationTokenSource = null;
            },
            _cancellationTokenSource.Token
        );

        //start a timer to start the fade out as well, with the same cancellation token
        Robust.Shared.Timing.Timer.Spawn(confirmableActionComponent.PrimeTime + confirmableActionComponent.ConfirmDelay - TimeSpan.FromSeconds(_cachedOverlay.fadeOutTime),
            () =>
            {
                _cachedOverlay.fadingOut = true;
            },
            _cancellationTokenSource.Token
        );
    }
}
