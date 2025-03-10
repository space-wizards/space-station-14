using Robust.Shared.Timing;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
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

    private Overlay? _cachedOverlay = null;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonumentPlacementMarkerComponent, ActionAttemptEvent>(OnAttemptMonumentPlacement);
    }

    //todo figure out a better system for this, this doesn't properly remove the overlay when the monument is placed
    private void OnAttemptMonumentPlacement(Entity<MonumentPlacementMarkerComponent> ent, ref ActionAttemptEvent args)
    {
        if (!TryComp<ConfirmableActionComponent>(ent, out var confirmableActionComponent))
            return; //return if the action somehow doesn't have a confirmableAction comp

        if (_cachedOverlay != null) //if we've already got a cached overlay, we've triggered the action and should remove the overlay todo just move this into a separate method that listens for the monument spawn?
        {
            _overlay.RemoveOverlay<MonumentPlacementPreviewOverlay>();
            _cachedOverlay = null;
        }

        _cachedOverlay = new MonumentPlacementPreviewOverlay(_entityManager, _playerManager, _spriteSystem, _transformSystem, _mapSystem, _tileDef, _lookup, _protoMan); //it's probably inefficient to make a new one every time, but this'll be happening like four times a round maybe
        _overlay.AddOverlay(_cachedOverlay);

        //remove the overlay automatically after the primeTime expires
        Timer.Spawn(confirmableActionComponent.PrimeTime + confirmableActionComponent.ConfirmDelay,
            () =>
            {
                _overlay.RemoveOverlay<MonumentPlacementPreviewOverlay>();
                _cachedOverlay = null;
            }
        );
    }
}
