using Content.Client.Atmos.EntitySystems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;

namespace Content.Client.Atmos.Overlays
{
    public sealed class FireTileOverlay : Overlay
    {
        private readonly GasTileOverlaySystem _gasTileOverlaySystem;

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
        private readonly ShaderInstance _shader;

        public FireTileOverlay()
        {
            IoCManager.InjectDependencies(this);

            _gasTileOverlaySystem = EntitySystem.Get<GasTileOverlaySystem>();
            _shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance().Duplicate();
            ZIndex = GasTileOverlaySystem.GasOverlayZIndex + 1;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var drawHandle = args.WorldHandle;

            var mapId = args.Viewport.Eye!.Position.MapId;
            var worldBounds = args.WorldBounds;

            drawHandle.UseShader(_shader);

            foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                if (!_gasTileOverlaySystem.HasData(mapGrid.Index))
                    continue;

                drawHandle.SetTransform(mapGrid.WorldMatrix);

                foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
                {
                    var enumerator = _gasTileOverlaySystem.GetFireOverlays(mapGrid.Index, tile.GridIndices);
                    while (enumerator.MoveNext(out var tuple))
                    {
                        drawHandle.DrawTexture(tuple.Texture, new Vector2(tile.X, tile.Y), tuple.Color);
                    }
                }
            }

            drawHandle.SetTransform(Matrix3.Identity);
        }
    }
}
