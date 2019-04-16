using System.Diagnostics.CodeAnalysis;
using Content.Client.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.GameObjects.Components.IconSmoothing
{
    // TODO: Potential improvements:
    //  Defer updating of these.
    //  Get told by somebody to use a loop.
    /// <summary>
    ///     Makes sprites of other grid-aligned entities like us connect.
    /// </summary>
    /// <remarks>
    ///     The system is based on Baystation12's smoothwalling, and thus will work with those.
    ///     To use, set <c>base</c> equal to the prefix of the corner states in the sprite base RSI.
    ///     Any objects with the same <c>key</c> will connect.
    /// </remarks>
    public sealed class IconSmoothComponent : Component
    {
        private string _smoothKey;
        private string _stateBase;
        private IconSmoothingMode _mode;

        public override string Name => "IconSmooth";

        internal ISpriteComponent Sprite { get; private set; }
        internal SnapGridComponent SnapGrid { get; private set; }
        private (GridId, MapIndices) _lastPosition;

        /// <summary>
        ///     We will smooth with other objects with the same key.
        /// </summary>
        public string SmoothKey => _smoothKey;

        /// <summary>
        ///     Prepended to the RSI state.
        /// </summary>
        public string StateBase => _stateBase;

        /// <summary>
        ///     Mode that controls how the icon should be selected.
        /// </summary>
        public IconSmoothingMode Mode => _mode;

        /// <summary>
        ///     Used by <see cref="IconSmoothSystem"/> to reduce redundant updates.
        /// </summary>
        internal int UpdateGeneration { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            SnapGrid = Owner.GetComponent<SnapGridComponent>();
            Sprite = Owner.GetComponent<ISpriteComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref _stateBase, "base", "");
            serializer.DataFieldCached(ref _smoothKey, "key", null);
            serializer.DataFieldCached(ref _mode, "mode", IconSmoothingMode.Corners);
        }

        public override void Startup()
        {
            base.Startup();

            SnapGrid.OnPositionChanged += SnapGridOnPositionChanged;
            Owner.EntityManager.RaiseEvent(Owner, new IconSmoothDirtyEvent(null, SnapGrid.Offset, Mode));
            var state0 = $"{StateBase}0";
            if (Mode == IconSmoothingMode.Corners)
            {
                Sprite.LayerMapSet(CornerLayers.SE, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(CornerLayers.SE, DirectionOffset.None);
                Sprite.LayerMapSet(CornerLayers.NE, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(CornerLayers.NE, DirectionOffset.CounterClockwise);
                Sprite.LayerMapSet(CornerLayers.NW, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(CornerLayers.NW, DirectionOffset.Flip);
                Sprite.LayerMapSet(CornerLayers.SW, Sprite.AddLayerState(state0));
                Sprite.LayerSetDirOffset(CornerLayers.SW, DirectionOffset.Clockwise);
            }
        }

        public override void Shutdown()
        {
            SnapGrid.OnPositionChanged -= SnapGridOnPositionChanged;
            Owner.EntityManager.RaiseEvent(Owner, new IconSmoothDirtyEvent(_lastPosition, SnapGrid.Offset, Mode));

            base.Shutdown();
        }

        private void SnapGridOnPositionChanged()
        {
            Owner.EntityManager.RaiseEvent(Owner, new IconSmoothDirtyEvent(_lastPosition, SnapGrid.Offset, Mode));
            _lastPosition = (Owner.Transform.GridID, SnapGrid.Position);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum CornerLayers
        {
            SE,
            NE,
            NW,
            SW,
        }
    }

    /// <summary>
    ///     Controls the mode with which icon smoothing is calculated.
    /// </summary>
    [PublicAPI]
    public enum IconSmoothingMode
    {
        /// <summary>
        ///     Each icon is made up of 4 corners, each of which can get a different state depending on
        ///     adjacent entities clockwise, counter-clockwise and diagonal with the corner.
        /// </summary>
        Corners,

        /// <summary>
        ///     There are 16 icons, only one of which is used at once.
        ///     The icon selected is a bit field made up of the cardinal direction flags that have adjacent entities.
        /// </summary>
        CardinalFlags,
    }
}
