using Content.Shared.Atmos.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Utility;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client.Atmos;

public sealed class AlignAtmosPipeLayers : PlacementMode
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTransformSystem _transformSystem;

    private const float SearchBoxSize = 2f;
    private EntityCoordinates _unalignedMouseCoords = default;

    private ISawmill _sawmill;

    /// <summary>
    /// This placement mode is not on the engine because it is content specific (i.e., for atmos pipes)
    /// </summary>
    public AlignAtmosPipeLayers(PlacementManager pMan) : base(pMan)
    {
        IoCManager.InjectDependencies(this);

        _mapSystem = _entityManager.System<SharedMapSystem>();
        _transformSystem = _entityManager.System<SharedTransformSystem>();

        _sawmill = _logManager.GetSawmill("placement");
    }

    public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
    {
        if (pManager.CurrentPermission?.EntityType == null)
            return;

        _unalignedMouseCoords = ScreenToCursorGrid(mouseScreen);
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

        // Try to get alternative prototypes from the entity atmos pipe layer component
        if (!_protoManager.TryIndex<EntityPrototype>(pManager.CurrentPermission.EntityType, out var currentProto))
            return;

        if (!currentProto.TryGetComponent<AtmosPipeLayersComponent>(out var atmosPipeLayers, _entityManager.ComponentFactory))
            return;

        if (atmosPipeLayers.AlternativePrototypes == null || atmosPipeLayers.AlternativePrototypes.Length == 0)
            return;

        // Calculate the position of the mouse cursor with respect to the center of the tile
        var mouseCoordsDiff = _unalignedMouseCoords.Position - MouseCoords.Position;
        var newProtoId = atmosPipeLayers.AlternativePrototypes[0];

        if (mouseCoordsDiff.Length() > 0.25f)
        {
            // Get the orientation of the player eye
            var eyeDir = _eyeManager.CurrentEye.Rotation.GetCardinalDir();

            // Determine the direction of the mouse is relative to the center of the tile,
            // adjusting for the player eye and grid rotation
            var direction = (new Angle(mouseCoordsDiff) + _eyeManager.CurrentEye.Rotation + gridRotation + Math.PI/2).GetCardinalDir();

            if (direction == Direction.North || direction == Direction.East)
            {
                // Use secondary config
                if (atmosPipeLayers.AlternativePrototypes.Length > 1)
                    newProtoId = atmosPipeLayers.AlternativePrototypes[1];
            }

            else
            {
                // Use tertiary config
                if (atmosPipeLayers.AlternativePrototypes.Length > 2)
                    newProtoId = atmosPipeLayers.AlternativePrototypes[2];
            }

            _sawmill.Log(LogLevel.Debug, "Mouse position within tile: " + direction.ToString());
        }

        else
        {
            _sawmill.Log(LogLevel.Debug, "Mouse position within tile: Center");
        }

        if (_protoManager.TryIndex<EntityPrototype>(newProtoId, out var newProto))
        {
            // Update the placed prototype
            pManager.CurrentPermission.EntityType = newProtoId;

            // Update the appearance of the ghost sprite
            if (newProto.TryGetComponent<SpriteComponent>(out var sprite, _entityManager.ComponentFactory))
            {
                var textures = new List<IDirectionalTextureProvider>();

                foreach (var layer in sprite.AllLayers)
                {
                    if (layer.ActualRsi?.Path != null && layer.RsiState.Name != null)
                        textures.Add(new SpriteSpecifier.Rsi(layer.ActualRsi.Path, layer.RsiState.Name).DirFrame0());
                }

                pManager.CurrentTextures = textures;
            }
        }
    }

    public override bool IsValidPosition(EntityCoordinates position)
    {
        return RangeCheck(position);
    }
}
