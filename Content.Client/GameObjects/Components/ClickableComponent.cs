#nullable enable
using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public sealed class ClickableComponent : Component
    {
        public override string Name => "Clickable";

        [Dependency] private readonly IClickMapManager _clickMapManager = default!;

        [ViewVariables] [DataField("bounds")] private DirBoundData _data = DirBoundData.Default;

        /// <summary>
        /// Used to check whether a click worked.
        /// </summary>
        /// <param name="worldPos">The world position that was clicked.</param>
        /// <param name="drawDepth">
        /// The draw depth for the sprite that captured the click.
        /// </param>
        /// <returns>True if the click worked, false otherwise.</returns>
        public bool CheckClick(Vector2 worldPos, out int drawDepth, out uint renderOrder)
        {
            if (!Owner.TryGetComponent(out ISpriteComponent? sprite) || !sprite.Visible)
            {
                drawDepth = default;
                renderOrder = default;
                return false;
            }

            var transform = Owner.Transform;
            var localPos = transform.InvWorldMatrix.Transform(worldPos);
            var spriteMatrix = Matrix3.Invert(sprite.GetLocalMatrix());

            localPos = spriteMatrix.Transform(localPos);

            var found = false;
            var worldRotation = transform.WorldRotation;

            if (_data.All.Contains(localPos))
            {
                found = true;
            }
            else
            {
                // TODO: diagonal support?

                var modAngle = sprite.NoRotation ? SpriteComponent.CalcRectWorldAngle(worldRotation, 4) : Angle.Zero;
                var dir = sprite.EnableDirectionOverride ? sprite.DirectionOverride : worldRotation.GetCardinalDir();

                modAngle += dir.ToAngle();

                var layerPos = modAngle.RotateVec(localPos);

                var boundsForDir = dir switch
                {
                    Direction.East => _data.East,
                    Direction.North => _data.North,
                    Direction.South => _data.South,
                    Direction.West => _data.West,
                    _ => throw new InvalidOperationException()
                };

                if (boundsForDir.Contains(layerPos))
                {
                    found = true;
                }
            }

            if (!found)
            {
                foreach (var layer in sprite.AllLayers)
                {
                    if (!layer.Visible) continue;

                    var dirCount = sprite.GetLayerDirectionCount(layer);
                    var dir = layer.EffectiveDirection(worldRotation);
                    var modAngle = sprite.NoRotation ? SpriteComponent.CalcRectWorldAngle(worldRotation, dirCount) : Angle.Zero;
                    modAngle += dir.Convert().ToAngle();

                    var layerPos = modAngle.RotateVec(localPos);

                    var localOffset = layerPos * EyeManager.PixelsPerMeter;

                    localOffset *= layer.DirOffset switch
                    {
                        SpriteComponent.DirectionOffset.None => (1, -1),
                        SpriteComponent.DirectionOffset.Clockwise => (-1, -1),
                        SpriteComponent.DirectionOffset.CounterClockwise => (1, 1),
                        SpriteComponent.DirectionOffset.Flip => (-1, 1),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    if (layer.Texture != null)
                    {
                        if (_clickMapManager.IsOccluding(layer.Texture,
                            (Vector2i) (localOffset + layer.Texture.Size / 2f)))
                        {
                            found = true;
                            break;
                        }
                    }
                    else if (layer.RsiState != default)
                    {
                        var rsi = layer.ActualRsi;
                        if (rsi == null)
                        {
                            continue;
                        }

                        var (mX, mY) = localOffset + rsi.Size / 2;
                        (mX, mY) = layer.DirOffset == SpriteComponent.DirectionOffset.Clockwise ||
                                   layer.DirOffset == SpriteComponent.DirectionOffset.CounterClockwise
                            ? (mY, mX)
                            : (mX, mY);

                        if (_clickMapManager.IsOccluding(rsi, layer.RsiState, dir,
                            layer.AnimationFrame, ((int) mX, (int) mY)))
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            drawDepth = sprite.DrawDepth;
            renderOrder = sprite.RenderOrder;
            return found;
        }

        [DataDefinition]
        public sealed class DirBoundData
        {
            [ViewVariables] [DataField("all")] public Box2 All;
            [ViewVariables] [DataField("north")] public Box2 North;
            [ViewVariables] [DataField("south")] public Box2 South;
            [ViewVariables] [DataField("east")] public Box2 East;
            [ViewVariables] [DataField("west")] public Box2 West;

            public static DirBoundData Default { get; } = new();
        }
    }
}
