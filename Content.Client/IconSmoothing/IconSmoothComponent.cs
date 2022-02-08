using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.IconSmoothing
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
    [RegisterComponent]
    public class IconSmoothComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        [DataField("mode")]
        private IconSmoothingMode _mode = IconSmoothingMode.Corners;

        internal ISpriteComponent? Sprite { get; private set; }

        public (GridId, Vector2i)? LastPosition;

        /// <summary>
        ///     We will smooth with other objects with the same key.
        /// </summary>
        [DataField("key")]
        public string? SmoothKey { get; }

        /// <summary>
        ///     Prepended to the RSI state.
        /// </summary>
        [DataField("base")]
        public string StateBase { get; } = string.Empty;

        /// <summary>
        ///     Mode that controls how the icon should be selected.
        /// </summary>
        public IconSmoothingMode Mode => _mode;

        /// <summary>
        ///     Used by <see cref="IconSmoothSystem"/> to reduce redundant updates.
        /// </summary>
        internal int UpdateGeneration { get; set; }

        protected override void Initialize()
        {
            base.Initialize();

            Sprite = _entMan.GetComponent<ISpriteComponent>(Owner);
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            if (_entMan.GetComponent<TransformComponent>(Owner).Anchored)
            {
                // ensures lastposition initial value is populated on spawn. Just calling
                // the hook here would cause a dirty event to fire needlessly
                UpdateLastPosition();
                EntitySystem.Get<IconSmoothSystem>().UpdateSmoothing(Owner, this);
            }

            if (Sprite != null && Mode == IconSmoothingMode.Corners)
            {
                var state0 = $"{StateBase}0";
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

        private void UpdateLastPosition()
        {
            var transform = _entMan.GetComponent<TransformComponent>(Owner);

            if (_mapManager.TryGetGrid(transform.GridID, out var grid))
            {
                LastPosition = (transform.GridID, grid.TileIndicesFor(transform.Coordinates));
            }
            else
            {
                // When this is called during component startup, the transform can end up being with an invalid grid ID.
                // In that case, use this.
                LastPosition = (GridId.Invalid, new Vector2i(0, 0));
            }
        }

        internal virtual void CalculateNewSprite()
        {
            var transform = _entMan.GetComponent<TransformComponent>(Owner);

            if (!transform.Anchored)
            {
                CalculateNewSprite(null);
                return;
            }

            if (!_mapManager.TryGetGrid(transform.GridID, out var grid))
            {
                Logger.Error($"Failed to calculate IconSmoothComponent sprite in {Owner} because grid {transform.GridID} was missing.");
                return;
            }
            CalculateNewSprite(grid);
        }

        internal virtual void CalculateNewSprite(IMapGrid? grid)
        {
            switch (Mode)
            {
                case IconSmoothingMode.Corners:
                    CalculateNewSpriteCorners(grid);
                    break;
                case IconSmoothingMode.CardinalFlags:
                    CalculateNewSpriteCardinal(grid);
                    break;
                case IconSmoothingMode.NoSprite:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CalculateNewSpriteCardinal(IMapGrid? grid)
        {
            if (Sprite == null)
            {
                return;
            }

            var dirs = CardinalConnectDirs.None;

            if (grid == null)
            {
                Sprite.LayerSetState(0, $"{StateBase}{(int) dirs}");
                return;
            }

            var position = _entMan.GetComponent<TransformComponent>(Owner).Coordinates;
            if (MatchingEntity(grid.GetInDir(position, Direction.North)))
                dirs |= CardinalConnectDirs.North;
            if (MatchingEntity(grid.GetInDir(position, Direction.South)))
                dirs |= CardinalConnectDirs.South;
            if (MatchingEntity(grid.GetInDir(position, Direction.East)))
                dirs |= CardinalConnectDirs.East;
            if (MatchingEntity(grid.GetInDir(position, Direction.West)))
                dirs |= CardinalConnectDirs.West;

            Sprite.LayerSetState(0, $"{StateBase}{(int) dirs}");
        }

        private void CalculateNewSpriteCorners(IMapGrid? grid)
        {
            if (Sprite == null)
            {
                return;
            }

            var (cornerNE, cornerNW, cornerSW, cornerSE) = CalculateCornerFill(grid);

            Sprite.LayerSetState(CornerLayers.NE, $"{StateBase}{(int) cornerNE}");
            Sprite.LayerSetState(CornerLayers.SE, $"{StateBase}{(int) cornerSE}");
            Sprite.LayerSetState(CornerLayers.SW, $"{StateBase}{(int) cornerSW}");
            Sprite.LayerSetState(CornerLayers.NW, $"{StateBase}{(int) cornerNW}");
        }

        protected (CornerFill ne, CornerFill nw, CornerFill sw, CornerFill se) CalculateCornerFill(IMapGrid? grid)
        {
            if (grid == null)
            {
                return (CornerFill.None, CornerFill.None, CornerFill.None, CornerFill.None);
            }

            var position = _entMan.GetComponent<TransformComponent>(Owner).Coordinates;
            var n = MatchingEntity(grid.GetInDir(position, Direction.North));
            var ne = MatchingEntity(grid.GetInDir(position, Direction.NorthEast));
            var e = MatchingEntity(grid.GetInDir(position, Direction.East));
            var se = MatchingEntity(grid.GetInDir(position, Direction.SouthEast));
            var s = MatchingEntity(grid.GetInDir(position, Direction.South));
            var sw = MatchingEntity(grid.GetInDir(position, Direction.SouthWest));
            var w = MatchingEntity(grid.GetInDir(position, Direction.West));
            var nw = MatchingEntity(grid.GetInDir(position, Direction.NorthWest));

            // ReSharper disable InconsistentNaming
            var cornerNE = CornerFill.None;
            var cornerSE = CornerFill.None;
            var cornerSW = CornerFill.None;
            var cornerNW = CornerFill.None;
            // ReSharper restore InconsistentNaming

            if (n)
            {
                cornerNE |= CornerFill.CounterClockwise;
                cornerNW |= CornerFill.Clockwise;
            }

            if (ne)
            {
                cornerNE |= CornerFill.Diagonal;
            }

            if (e)
            {
                cornerNE |= CornerFill.Clockwise;
                cornerSE |= CornerFill.CounterClockwise;
            }

            if (se)
            {
                cornerSE |= CornerFill.Diagonal;
            }

            if (s)
            {
                cornerSE |= CornerFill.Clockwise;
                cornerSW |= CornerFill.CounterClockwise;
            }

            if (sw)
            {
                cornerSW |= CornerFill.Diagonal;
            }

            if (w)
            {
                cornerSW |= CornerFill.Clockwise;
                cornerNW |= CornerFill.CounterClockwise;
            }

            if (nw)
            {
                cornerNW |= CornerFill.Diagonal;
            }

            // Local is fine as we already know it's parented to the grid (due to the way anchoring works).
            switch (_entMan.GetComponent<TransformComponent>(Owner).LocalRotation.GetCardinalDir())
            {
                case Direction.North:
                    return (cornerSW, cornerSE, cornerNE, cornerNW);
                case Direction.West:
                    return (cornerSE, cornerNE, cornerNW, cornerSW);
                case Direction.South:
                    return (cornerNE, cornerNW, cornerSW, cornerSE);
                default:
                    return (cornerNW, cornerSW, cornerSE, cornerNE);
            }
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            EntitySystem.Get<IconSmoothSystem>().UpdateSmoothing(Owner, this);
        }

        [System.Diagnostics.Contracts.Pure]
        protected bool MatchingEntity(IEnumerable<EntityUid> candidates)
        {
            foreach (var entity in candidates)
            {
                if (!_entMan.TryGetComponent(entity, out IconSmoothComponent? other))
                {
                    continue;
                }

                if (other.SmoothKey == SmoothKey)
                {
                    return true;
                }
            }

            return false;
        }

        [Flags]
        private enum CardinalConnectDirs : byte
        {
            None = 0,
            North = 1,
            South = 2,
            East = 4,
            West = 8
        }

        [Flags]
        public enum CornerFill : byte
        {
            // These values are pulled from Baystation12.
            // I'm too lazy to convert the state names.
            None = 0,

            // The cardinal tile counter-clockwise of this corner is filled.
            CounterClockwise = 1,

            // The diagonal tile in the direction of this corner.
            Diagonal = 2,

            // The cardinal tile clockwise of this corner is filled.
            Clockwise = 4,
        }

        public enum CornerLayers : byte
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
    public enum IconSmoothingMode : byte
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

        /// <summary>
        ///     Where this component contributes to our neighbors being calculated but we do not update our own sprite.
        /// </summary>
        NoSprite,
    }
}
