using System;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using static SS14.Client.GameObjects.SpriteComponent;

namespace Content.Client.GameObjects.Components.SmoothWalling
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
    public class IconSmoothComponent : Component, IComponentDebug
    {
        public override string Name => "IconSmooth";

        ISpriteComponent Sprite;
        SnapGridComponent SnapGrid;

        /// <summary>
        ///     Prepended to the RSI state.
        /// </summary>
        string StateBase;

        /// <summary>
        ///     We will smooth with other objects with the same key.
        /// </summary>
        string SmoothKey;

        IconSmoothComponent[] Neighbors = new IconSmoothComponent[8];
        // "Use an array".
        // Nah. I'm too lazy. This is easy to understand compared to the enum value fuckery if I used an array.
        // Deal with it.
        CornerFill CornerSE;
        CornerFill CornerNE;
        CornerFill CornerNW;
        CornerFill CornerSW;

        public override void Initialize()
        {
            base.Initialize();

            var state0 = $"{StateBase}0";
            SnapGrid = Owner.GetComponent<SnapGridComponent>();
            Sprite = Owner.GetComponent<ISpriteComponent>();
            // BIG NOTE: Y axis is fucked. Double fucked. Triple super-mega-ultra-turbo-fucked.
            // so, the DirectionOffsets here are incorrect (flipped).
            Sprite.LayerMapSet(CornerLayers.SE, Sprite.AddLayerState(state0));
            Sprite.LayerSetDirOffset(CornerLayers.SE, DirectionOffset.Flip);
            Sprite.LayerMapSet(CornerLayers.NE, Sprite.AddLayerState(state0));
            Sprite.LayerSetDirOffset(CornerLayers.NE, DirectionOffset.Clockwise);
            Sprite.LayerMapSet(CornerLayers.NW, Sprite.AddLayerState(state0));
            Sprite.LayerSetDirOffset(CornerLayers.NW, DirectionOffset.None);
            Sprite.LayerMapSet(CornerLayers.SW, Sprite.AddLayerState(state0));
            Sprite.LayerSetDirOffset(CornerLayers.SW, DirectionOffset.CounterClockwise);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataFieldCached(ref StateBase, "base", "");
            serializer.DataFieldCached(ref SmoothKey, "key", null);
        }

        public override void Startup()
        {
            base.Startup();

            SnapGrid.OnPositionChanged += SnapGridPositionChanged;

            UpdateConnections(true);
            UpdateIcon();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            SnapGrid.OnPositionChanged -= SnapGridPositionChanged;
            SayGoodbyes();
        }

        void SayGoodbyes()
        {
            foreach (var neighbor in Neighbors)
            {
                // Goodbye neighbor.
                neighbor?.UpdateConnections(false);
                neighbor?.UpdateIcon();
            }
        }

        void SnapGridPositionChanged()
        {
            SayGoodbyes();
            UpdateConnections(true);
            UpdateIcon();
        }

        void UpdateIcon()
        {
            // Try to turn this into a loop without hard to understand bit fuckery or 20 lines of helper functions.
            // Challenge: do it in less lines.
            // I dare you.
            // No cheating like putting everything on a single line. Proper code conventions.
            // This comment does not count, btw.

            Sprite.LayerSetState(CornerLayers.NE, $"{StateBase}{(int)CornerNE}");
            Sprite.LayerSetState(CornerLayers.SE, $"{StateBase}{(int)CornerSE}");
            Sprite.LayerSetState(CornerLayers.SW, $"{StateBase}{(int)CornerSW}");
            Sprite.LayerSetState(CornerLayers.NW, $"{StateBase}{(int)CornerNW}");
        }

        void UpdateConnections(bool propagate)
        {
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var found = false;
                var dir = (Direction)i;
                foreach (var entity in SnapGrid.GetInDir(dir))
                {
                    if (entity.TryGetComponent(out IconSmoothComponent smooth) && smooth.SmoothKey == SmoothKey)
                    {
                        Neighbors[i] = smooth;
                        if (propagate)
                        {
                            smooth.UpdateConnections(false);
                            smooth.UpdateIcon();
                        }
                        // Temptation to use goto: 10.
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Neighbors[i] = null;
                }
            }

            CornerNE = CornerSE = CornerNW = CornerSW = CornerFill.None;

            // "Use a loop".
            // Well screw that I did the exact same thing while writing lighting corner population code in BYOND.
            // This is 10x easier to understand and write than a complex loop working with the internet mapping of direction flags.
            if (Neighbors[(int)Direction.North] != null)
            {
                CornerNE |= CornerFill.CounterClockwise;
                CornerNW |= CornerFill.Clockwise;
            }
            if (Neighbors[(int)Direction.NorthEast] != null)
            {
                CornerNE |= CornerFill.Diagonal;
            }
            if (Neighbors[(int)Direction.East] != null)
            {
                CornerNE |= CornerFill.Clockwise;
                CornerSE |= CornerFill.CounterClockwise;
            }
            if (Neighbors[(int)Direction.SouthEast] != null)
            {
                CornerSE |= CornerFill.Diagonal;
            }
            if (Neighbors[(int)Direction.South] != null)
            {
                CornerSE |= CornerFill.Clockwise;
                CornerSW |= CornerFill.CounterClockwise;
            }
            if (Neighbors[(int)Direction.SouthWest] != null)
            {
                CornerSW |= CornerFill.Diagonal;
            }
            if (Neighbors[(int)Direction.West] != null)
            {
                CornerSW |= CornerFill.Clockwise;
                CornerNW |= CornerFill.CounterClockwise;
            }
            if (Neighbors[(int)Direction.NorthWest] != null)
            {
                CornerNW |= CornerFill.Diagonal;
            }
        }

        public string GetDebugString()
        {
            return string.Format(
                "N/NE/E/SE/S/SW/W/NW: {0}/{1}/{2}/{3}/{4}/{5}/{6}/{7} cfill NE/SE/SW/NW: {8}/{9}/{10}/{11}",
                Neighbors[(int)Direction.North]?.Owner?.Uid,
                Neighbors[(int)Direction.NorthEast]?.Owner?.Uid,
                Neighbors[(int)Direction.East]?.Owner?.Uid,
                Neighbors[(int)Direction.SouthEast]?.Owner?.Uid,
                Neighbors[(int)Direction.South]?.Owner?.Uid,
                Neighbors[(int)Direction.SouthWest]?.Owner?.Uid,
                Neighbors[(int)Direction.West]?.Owner?.Uid,
                Neighbors[(int)Direction.NorthWest]?.Owner?.Uid,
                CornerNE, CornerSE, CornerSW, CornerNW
            );
        }

        enum CornerLayers
        {
            SE,
            NE,
            NW,
            SW,
        }

        [Flags]
        enum CornerFill : byte
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
    }
}
