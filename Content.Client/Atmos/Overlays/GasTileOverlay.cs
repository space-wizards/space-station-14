using Content.Client.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Atmos.Overlays
{
    public sealed class GasTileOverlay : Overlay
    {
        private readonly GasTileOverlaySystem _gasTileOverlaySystem;

        [Dependency] private readonly IMapManager _mapManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

        public GasTileOverlay()
        {
            IoCManager.InjectDependencies(this);

            _gasTileOverlaySystem = EntitySystem.Get<GasTileOverlaySystem>();
            ZIndex = GasTileOverlaySystem.GasOverlayZIndex;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var drawHandle = args.WorldHandle;

            var mapId = args.Viewport.Eye!.Position.MapId;
            var worldBounds = args.WorldBounds;

            foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                if (!_gasTileOverlaySystem.HasData(mapGrid.Index))
                    continue;

                drawHandle.SetTransform(mapGrid.WorldMatrix);

                foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
                {
                    var enumerator = _gasTileOverlaySystem.GetOverlays(mapGrid.Index, tile.GridIndices);
                    while (enumerator.MoveNext(out var tuple))
                    {
                        drawHandle.DrawTexture(tuple.Texture, new Vector2(tile.X, tile.Y), tuple.Color);
                    }
                }
            }
        }
    }
}
