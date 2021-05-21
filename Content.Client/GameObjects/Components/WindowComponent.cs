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
        private string? _stateBase;

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
                const string cracksRSIPath = "/Textures/Constructible/Structures/Windows/cracks.rsi";
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

            if (_sprite != null)
            {
                _sprite.LayerSetState(CornerLayers.NE, $"{_stateBase}{(int) lowWall.LastCornerNE}");
                _sprite.LayerSetState(CornerLayers.SE, $"{_stateBase}{(int) lowWall.LastCornerSE}");
                _sprite.LayerSetState(CornerLayers.SW, $"{_stateBase}{(int) lowWall.LastCornerSW}");
                _sprite.LayerSetState(CornerLayers.NW, $"{_stateBase}{(int) lowWall.LastCornerNW}");
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
}
