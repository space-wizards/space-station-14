using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using static Content.Client.GameObjects.Components.IconSmoothing.IconSmoothComponent;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public sealed class WindowComponent : SharedWindowComponent
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        [DataField("base")]
        private string _stateBase = "window";

        [DataField("overlayRsi")]
        private string _overlayRsiPath = "/Textures/Constructible/Structures/Walls/low_wall.rsi";

        [DataField("cracksRsi")]
        private string cracksRSIPath = "/Textures/Constructible/Structures/Windows/cracks.rsi";

        private ISpriteComponent? _sprite;

        public override void Initialize()
        {
            base.Initialize();

            _sprite = Owner.GetComponent<ISpriteComponent>();
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new WindowSmoothDirtyEvent(Owner));

            if (_sprite != null)
            {
                var state0 = $"{_stateBase}0";
                const string defaultOverlayState0 = "metal_over_0"; // Overriden when placed with an actual low wall, so doesn't matter what it is
                _sprite.LayerMapSet(CornerLayers.SE, _sprite.AddLayerState(state0));
                _sprite.LayerSetDirOffset(CornerLayers.SE, SpriteComponent.DirectionOffset.None);
                _sprite.LayerMapSet(WindowDamageLayers.DamageSE, _sprite.AddLayerState("0_1", cracksRSIPath));
                _sprite.LayerSetVisible(WindowDamageLayers.DamageSE, false);

                _sprite.LayerMapSet(CornerLayers.NE, _sprite.AddLayerState(state0));
                _sprite.LayerSetDirOffset(CornerLayers.NE, SpriteComponent.DirectionOffset.CounterClockwise);
                _sprite.LayerMapSet(WindowDamageLayers.DamageNE, _sprite.AddLayerState("0_1", cracksRSIPath));
                _sprite.LayerSetDirOffset(WindowDamageLayers.DamageNE, SpriteComponent.DirectionOffset.CounterClockwise);
                _sprite.LayerSetVisible(WindowDamageLayers.DamageNE, false);

                _sprite.LayerMapSet(CornerLayers.NW, _sprite.AddLayerState(state0));
                _sprite.LayerSetDirOffset(CornerLayers.NW, SpriteComponent.DirectionOffset.Flip);
                _sprite.LayerMapSet(WindowDamageLayers.DamageNW, _sprite.AddLayerState("0_1", cracksRSIPath));
                _sprite.LayerSetDirOffset(WindowDamageLayers.DamageNW, SpriteComponent.DirectionOffset.Flip);
                _sprite.LayerSetVisible(WindowDamageLayers.DamageNW, false);

                _sprite.LayerMapSet(CornerLayers.SW, _sprite.AddLayerState(state0));
                _sprite.LayerSetDirOffset(CornerLayers.SW, SpriteComponent.DirectionOffset.Clockwise);
                _sprite.LayerMapSet(WindowDamageLayers.DamageSW, _sprite.AddLayerState("0_1", cracksRSIPath));
                _sprite.LayerSetDirOffset(WindowDamageLayers.DamageSW, SpriteComponent.DirectionOffset.Clockwise);
                _sprite.LayerSetVisible(WindowDamageLayers.DamageSW, false);

                // Wall overlay layers
                _sprite.LayerMapSet(OverlayCornerLayers.SE, _sprite.AddLayerState(defaultOverlayState0, _overlayRsiPath));
                _sprite.LayerSetDirOffset(OverlayCornerLayers.SE, SpriteComponent.DirectionOffset.None);
                _sprite.LayerMapSet(OverlayCornerLayers.NE, _sprite.AddLayerState(defaultOverlayState0, _overlayRsiPath));
                _sprite.LayerSetDirOffset(OverlayCornerLayers.NE, SpriteComponent.DirectionOffset.CounterClockwise);
                _sprite.LayerMapSet(OverlayCornerLayers.NW, _sprite.AddLayerState(defaultOverlayState0, _overlayRsiPath));
                _sprite.LayerSetDirOffset(OverlayCornerLayers.NW, SpriteComponent.DirectionOffset.Flip);
                _sprite.LayerMapSet(OverlayCornerLayers.SW, _sprite.AddLayerState(defaultOverlayState0, _overlayRsiPath));
                _sprite.LayerSetDirOffset(OverlayCornerLayers.SW, SpriteComponent.DirectionOffset.Clockwise);
            }
        }

        public void SnapGridOnPositionChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new WindowSmoothDirtyEvent(Owner));
        }

        public void UpdateSprite()
        {
            var lowWall = FindLowWall();
            if (lowWall == null)
            {
                return;
            }

            if (_sprite != null && lowWall.Owner.TryGetComponent<SpriteComponent>(out var wallSprite))
            {
                // How the window actually looks
                _sprite.LayerSetState(CornerLayers.NE, $"{_stateBase}{(int) lowWall.LastWallCornerNE}");
                _sprite.LayerSetState(CornerLayers.SE, $"{_stateBase}{(int) lowWall.LastWallCornerSE}");
                _sprite.LayerSetState(CornerLayers.SW, $"{_stateBase}{(int) lowWall.LastWallCornerSW}");
                _sprite.LayerSetState(CornerLayers.NW, $"{_stateBase}{(int) lowWall.LastWallCornerNW}");

                // The low wall overlays on top of the window, rendered to add depth
                _sprite.LayerSetState(OverlayCornerLayers.NE, $"{lowWall.StateBase}over_{(int) lowWall.LastOverlayCornerNE}", _overlayRsiPath);
                _sprite.LayerSetState(OverlayCornerLayers.SE, $"{lowWall.StateBase}over_{(int) lowWall.LastOverlayCornerSE}", _overlayRsiPath);
                _sprite.LayerSetState(OverlayCornerLayers.SW, $"{lowWall.StateBase}over_{(int) lowWall.LastOverlayCornerSW}", _overlayRsiPath);
                _sprite.LayerSetState(OverlayCornerLayers.NW, $"{lowWall.StateBase}over_{(int) lowWall.LastOverlayCornerNW}", _overlayRsiPath);
                _sprite.LayerSetColor(OverlayCornerLayers.NE, wallSprite.Color);
                _sprite.LayerSetColor(OverlayCornerLayers.SE, wallSprite.Color);
                _sprite.LayerSetColor(OverlayCornerLayers.SW, wallSprite.Color);
                _sprite.LayerSetColor(OverlayCornerLayers.NW, wallSprite.Color);
            }
        }

        private LowWallComponent? FindLowWall()
        {
            if (!Owner.Transform.Anchored)
                return null;

            var grid = _mapManager.GetGrid(Owner.Transform.GridID);
            var coords = Owner.Transform.Coordinates;
            foreach (var entity in grid.GetLocal(coords))
            {
                if (Owner.EntityManager.ComponentManager.TryGetComponent(entity, out LowWallComponent? lowWall))
                {
                    return lowWall;
                }
            }

            return null;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WindowDamageLayers : byte
    {
        DamageSE,
        DamageNE,
        DamageNW,
        DamageSW
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum OverlayCornerLayers : byte
    {
        SE,
        NE,
        NW,
        SW,
    }
}
