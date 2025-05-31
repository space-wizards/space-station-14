using Content.Client.Construction;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Construction.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;
using static Robust.Client.Placement.PlacementManager;

namespace Content.Client.Atmos;

/// <summary>
/// Allows users to place atmos pipes on different layers depending on how the mouse cursor is positioned within a grid tile.
/// </summary>
/// <remarks>
/// This placement mode is not on the engine because it is content specific.
/// </remarks>
public sealed class AlignAtmosPipeLayers : SnapgridCenter
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTransformSystem _transformSystem;
    private readonly SharedAtmosPipeLayersSystem _pipeLayersSystem;
    private readonly SpriteSystem _spriteSystem;

    private const float SearchBoxSize = 2f;
    private EntityCoordinates _unalignedMouseCoords = default;
    private const float MouseDeadzoneRadius = 0.25f;

    private Color _guideColor = new Color(0, 0, 0.5785f);
    private const float GuideRadius = 0.1f;
    private const float GuideOffset = 0.21875f;

    public AlignAtmosPipeLayers(PlacementManager pMan) : base(pMan)
    {
        IoCManager.InjectDependencies(this);

        _mapSystem = _entityManager.System<SharedMapSystem>();
        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _pipeLayersSystem = _entityManager.System<SharedAtmosPipeLayersSystem>();
        _spriteSystem = _entityManager.System<SpriteSystem>();
    }

    /// <inheritdoc/>
    public override void Render(in OverlayDrawArgs args)
    {
        var gridUid = _entityManager.System<SharedTransformSystem>().GetGrid(MouseCoords);

        if (gridUid == null || Grid == null)
            return;

        // Draw guide circles for each pipe layer if we are not in line/grid placing mode
        if (pManager.PlacementType == PlacementTypes.None)
        {
            var gridRotation = _transformSystem.GetWorldRotation(gridUid.Value);
            var worldPosition = _mapSystem.LocalToWorld(gridUid.Value, Grid, MouseCoords.Position);
            var direction = (_eyeManager.CurrentEye.Rotation + gridRotation + Math.PI / 2).GetCardinalDir();
            var multi = (direction == Direction.North || direction == Direction.South) ? -1f : 1f;

            args.WorldHandle.DrawCircle(worldPosition, GuideRadius, _guideColor);
            args.WorldHandle.DrawCircle(worldPosition + gridRotation.RotateVec(new Vector2(multi * GuideOffset, GuideOffset)), GuideRadius, _guideColor);
            args.WorldHandle.DrawCircle(worldPosition - gridRotation.RotateVec(new Vector2(multi * GuideOffset, GuideOffset)), GuideRadius, _guideColor);
        }

        base.Render(args);
    }

    /// <inheritdoc/>
    public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
    {
        _unalignedMouseCoords = ScreenToCursorGrid(mouseScreen);
        base.AlignPlacementMode(mouseScreen);

        // Exit early if we are in line/grid placing mode
        if (pManager.PlacementType != PlacementTypes.None)
            return;

        MouseCoords = _unalignedMouseCoords.AlignWithClosestGridTile(SearchBoxSize, _entityManager, _mapManager);

        var gridId = _transformSystem.GetGrid(MouseCoords);

        if (!_entityManager.TryGetComponent<MapGridComponent>(gridId, out var mapGrid))
            return;

        var gridRotation = _transformSystem.GetWorldRotation(gridId.Value);
        CurrentTile = _mapSystem.GetTileRef(gridId.Value, mapGrid, MouseCoords);

        float tileSize = mapGrid.TileSize;
        GridDistancing = tileSize;

        MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
            CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y));

        // Calculate the position of the mouse cursor with respect to the center of the tile to determine which layer to use
        var mouseCoordsDiff = _unalignedMouseCoords.Position - MouseCoords.Position;
        var layer = AtmosPipeLayer.Primary;

        if (mouseCoordsDiff.Length() > MouseDeadzoneRadius)
        {
            // Determine the direction of the mouse is relative to the center of the tile, adjusting for the player eye and grid rotation
            var direction = (new Angle(mouseCoordsDiff) + _eyeManager.CurrentEye.Rotation + gridRotation + Math.PI / 2).GetCardinalDir();
            layer = (direction == Direction.North || direction == Direction.East) ? AtmosPipeLayer.Secondary : AtmosPipeLayer.Tertiary;
        }

        // Update the construction menu placer
        if (pManager.Hijack != null)
            UpdateHijackedPlacer(layer, mouseScreen);

        // Otherwise update the debug placer
        else
            UpdatePlacer(layer);
    }

    private void UpdateHijackedPlacer(AtmosPipeLayer layer, ScreenCoordinates mouseScreen)
    {
        // Try to get alternative prototypes from the construction prototype
        var constructionSystem = (pManager.Hijack as ConstructionPlacementHijack)?.CurrentConstructionSystem;
        var altPrototypes = (pManager.Hijack as ConstructionPlacementHijack)?.CurrentPrototype?.AlternativePrototypes;

        if (constructionSystem == null || altPrototypes == null || (int)layer >= altPrototypes.Length)
            return;

        var newProtoId = altPrototypes[(int)layer];

        if (!_protoManager.TryIndex(newProtoId, out var newProto))
            return;

        if (newProto.Type != ConstructionType.Structure)
        {
            pManager.Clear();
            return;
        }

        if (newProto.ID == (pManager.Hijack as ConstructionPlacementHijack)?.CurrentPrototype?.ID)
            return;

        // Start placing
        pManager.BeginPlacing(new PlacementInformation()
        {
            IsTile = false,
            PlacementOption = newProto.PlacementMode,
        }, new ConstructionPlacementHijack(constructionSystem, newProto));

        if (pManager.CurrentMode is AlignAtmosPipeLayers { } newMode)
            newMode.RefreshGrid(mouseScreen);

        // Update construction guide
        constructionSystem.GetGuide(newProto);
    }

    private void UpdatePlacer(AtmosPipeLayer layer)
    {
        // Try to get alternative prototypes from the entity atmos pipe layer component
        if (pManager.CurrentPermission?.EntityType == null)
            return;

        if (!_protoManager.TryIndex<EntityPrototype>(pManager.CurrentPermission.EntityType, out var currentProto))
            return;

        if (!currentProto.TryGetComponent<AtmosPipeLayersComponent>(out var atmosPipeLayers, _entityManager.ComponentFactory))
            return;

        if (!_pipeLayersSystem.TryGetAlternativePrototype(atmosPipeLayers, layer, out var newProtoId))
            return;

        if (_protoManager.TryIndex<EntityPrototype>(newProtoId, out var newProto))
        {
            // Update the placed prototype
            pManager.CurrentPermission.EntityType = newProtoId;

            // Update the appearance of the ghost sprite
            if (newProto.TryGetComponent<SpriteComponent>(out var sprite, _entityManager.ComponentFactory))
            {
                var textures = new List<IDirectionalTextureProvider>();

                foreach (var spriteLayer in sprite.AllLayers)
                {
                    if (spriteLayer.ActualRsi?.Path != null && spriteLayer.RsiState.Name != null)
                        textures.Add(_spriteSystem.RsiStateLike(new SpriteSpecifier.Rsi(spriteLayer.ActualRsi.Path, spriteLayer.RsiState.Name)));
                }

                pManager.CurrentTextures = textures;
            }
        }
    }

    private void RefreshGrid(ScreenCoordinates mouseScreen)
    {
        base.AlignPlacementMode(mouseScreen);
    }
}
