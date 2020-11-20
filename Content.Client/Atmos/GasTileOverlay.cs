using Content.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;

namespace Content.Client.Atmos
{
    public class GasTileOverlay : Overlay
    {
        private readonly GasTileOverlaySystem _gasTileOverlaySystem;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IClyde _clyde = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private List<ShaderInstance> _idleShaders = new List<ShaderInstance>();
        private List<ShaderInstance> _usedShaders = new List<ShaderInstance>();

        public GasTileOverlay() : base(nameof(GasTileOverlay))
        {
            IoCManager.InjectDependencies(this);
            _gasTileOverlaySystem = EntitySystem.Get<GasTileOverlaySystem>();
        }

        protected override void Draw(DrawingHandleBase handle, OverlaySpace overlay)
        {
            var drawHandle = (DrawingHandleWorld) handle;

            var mapId = _eyeManager.CurrentMap;
            var eye = _eyeManager.CurrentEye;

            var worldBounds = Box2.CenteredAround(eye.Position.Position,
                _clyde.ScreenSize / (float) EyeManager.PixelsPerMeter * eye.Zoom);

            var intersectingGrids = _mapManager.FindGridsIntersecting(mapId, worldBounds);
            foreach (var mapGrid in intersectingGrids)
            {
                if (!_gasTileOverlaySystem.HasData(mapGrid.Index))
                    continue;

                var gridBounds = new Box2(mapGrid.WorldToLocal(worldBounds.BottomLeft), mapGrid.WorldToLocal(worldBounds.TopRight));
                
                foreach (var tile in mapGrid.GetTilesIntersecting(gridBounds))
                {
                    foreach (var (texture, color) in _gasTileOverlaySystem.GetOverlays(mapGrid.Index, tile.GridIndices))
                    {
                        drawHandle.DrawTexture(texture, mapGrid.LocalToWorld(new Vector2(tile.X, tile.Y)), color);
                    }
                }
            }
            ResetShaderInstances();
            var qq = GetShaderInstance();
            drawHandle.UseShader(qq);
            var viewport = _eyeManager.GetWorldViewport();
            drawHandle.DrawRect(viewport, Color.White);
            return; 
            ResetShaderInstances();
            foreach (var mapGrid in intersectingGrids)
            {
                if (!_gasTileOverlaySystem.HasData(mapGrid.Index))
                    continue;

                var gridBounds = new Box2(mapGrid.WorldToLocal(worldBounds.BottomLeft), mapGrid.WorldToLocal(worldBounds.TopRight));

                foreach (var tile in mapGrid.GetTilesIntersecting(gridBounds))
                {
                    var shader = GetShaderInstance();
                    drawHandle.UseShader(shader);
                    var (fireTexture, fireColors) = _gasTileOverlaySystem.GetFireOverlay(mapGrid.Index, tile.GridIndices);
                    shader?.SetParameter("mainColor", fireColors[0]);
                    shader?.SetParameter("colorTop", fireColors[1]);
                    shader?.SetParameter("colorRight", fireColors[2]);
                    shader?.SetParameter("colorBottom", fireColors[3]);
                    shader?.SetParameter("colorLeft", fireColors[4]);
                    drawHandle.DrawTexture(fireTexture, mapGrid.LocalToWorld(new Vector2(tile.X, tile.Y)));
                }
            }
        }

        private ShaderInstance GetShaderInstance()
        {
            if (_idleShaders.Count > 0)
            {
                var shader = _idleShaders[0];
                _idleShaders.RemoveAt(0);
                _usedShaders.Add(shader);
                return shader;
            }
            else
            {
                var shader = _prototypeManager.Index<ShaderPrototype>("AtmosFire").Instance().Duplicate();
                _usedShaders.Add(shader);
                return shader;
            }
        }

        private void ResetShaderInstances()
        {
            for (int i = 0; i < _usedShaders.Count; i++)
            {
                _idleShaders.Add(_usedShaders[i]);
                _usedShaders.RemoveAt(i);
                i--;
            }
        }
    }
}
