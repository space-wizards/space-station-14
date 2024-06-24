using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Graphics;
using static Robust.Client.GameObjects.SpriteComponent;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Clickable
{
    [RegisterComponent]
    public sealed partial class ClickableComponent : Component
    {
        [Dependency] private readonly IClickMapManager _clickMapManager = default!;

        [DataField("bounds")] public DirBoundData? Bounds;

        /// <summary>
        /// Used to check whether a click worked. Will first check if the click falls inside of some explicit bounding
        /// boxes (see <see cref="Bounds"/>). If that fails, attempts to use automatically generated click maps.
        /// </summary>
        /// <param name="worldPos">The world position that was clicked.</param>
        /// <param name="drawDepth">
        /// The draw depth for the sprite that captured the click.
        /// </param>
        /// <returns>True if the click worked, false otherwise.</returns>
        public bool CheckClick(SpriteComponent sprite, TransformComponent transform, EntityQuery<TransformComponent> xformQuery, Vector2 worldPos, IEye eye, out int drawDepth, out uint renderOrder, out float bottom)
        {
            if (!sprite.Visible)
            {
                drawDepth = default;
                renderOrder = default;
                bottom = default;
                return false;
            }

            drawDepth = sprite.DrawDepth;
            renderOrder = sprite.RenderOrder;
            var (spritePos, spriteRot) = transform.GetWorldPositionRotation(xformQuery);
            var spriteBB = sprite.CalculateRotatedBoundingBox(spritePos, spriteRot, eye.Rotation);
            bottom = Matrix3Helpers.CreateRotation(eye.Rotation).TransformBox(spriteBB).Bottom;

            Matrix3x2.Invert(sprite.GetLocalMatrix(), out var invSpriteMatrix);

            // This should have been the rotation of the sprite relative to the screen, but this is not the case with no-rot or directional sprites.
            var relativeRotation = (spriteRot + eye.Rotation).Reduced().FlipPositive();

            Angle cardinalSnapping = sprite.SnapCardinals ? relativeRotation.GetCardinalDir().ToAngle() : Angle.Zero;

            // First we get `localPos`, the clicked location in the sprite-coordinate frame.
            var entityXform = Matrix3Helpers.CreateInverseTransform(transform.WorldPosition, sprite.NoRotation ? -eye.Rotation : spriteRot - cardinalSnapping);
            var localPos = Vector2.Transform(Vector2.Transform(worldPos, entityXform), invSpriteMatrix);

            // Check explicitly defined click-able bounds
            if (CheckDirBound(sprite, relativeRotation, localPos))
                return true;

            // Next check each individual sprite layer using automatically computed click maps.
            foreach (var spriteLayer in sprite.AllLayers)
            {
                if (!spriteLayer.Visible || spriteLayer is not Layer layer)
                    continue;

                // Check the layer's texture, if it has one
                if (layer.Texture != null)
                {
                    // Convert to image coordinates
                    var imagePos = (Vector2i) (localPos * EyeManager.PixelsPerMeter * new Vector2(1, -1) + layer.Texture.Size / 2f);

                    if (_clickMapManager.IsOccluding(layer.Texture, imagePos))
                        return true;
                }

                // Either we weren't clicking on the texture, or there wasn't one. In which case: check the RSI next
                if (layer.ActualRsi is not { } rsi || !rsi.TryGetState(layer.State, out var rsiState))
                    continue;

                var dir = Layer.GetDirection(rsiState.RsiDirections, relativeRotation);

                // convert to layer-local coordinates
                layer.GetLayerDrawMatrix(dir, out var matrix);
                Matrix3x2.Invert(matrix, out var inverseMatrix);
                var layerLocal = Vector2.Transform(localPos, inverseMatrix);

                // Convert to image coordinates
                var layerImagePos = (Vector2i) (layerLocal * EyeManager.PixelsPerMeter * new Vector2(1, -1) + rsiState.Size / 2f);

                // Next, to get the right click map we need the "direction" of this layer that is actually being used to draw the sprite on the screen.
                // This **can** differ from the dir defined before, but can also just be the same.
                if (sprite.EnableDirectionOverride)
                    dir = sprite.DirectionOverride.Convert(rsiState.RsiDirections);
                dir = dir.OffsetRsiDir(layer.DirOffset);

                if (_clickMapManager.IsOccluding(layer.ActualRsi!, layer.State, dir, layer.AnimationFrame, layerImagePos))
                    return true;
            }

            drawDepth = default;
            renderOrder = default;
            bottom = default;
            return false;
        }

        public bool CheckDirBound(SpriteComponent sprite, Angle relativeRotation, Vector2 localPos)
        {
            if (Bounds == null)
                return false;

            // These explicit bounds only work for either 1 or 4 directional sprites.

            // This would be the orientation of a 4-directional sprite.
            var direction = relativeRotation.GetCardinalDir();

            var modLocalPos = sprite.NoRotation
                ? localPos
                : direction.ToAngle().RotateVec(localPos);

            // First, check the bounding box that is valid for all orientations
            if (Bounds.All.Contains(modLocalPos))
                return true;

            // Next, get and check the appropriate bounding box for the current sprite orientation
            var boundsForDir = (sprite.EnableDirectionOverride ? sprite.DirectionOverride : direction) switch
            {
                Direction.East => Bounds.East,
                Direction.North => Bounds.North,
                Direction.South => Bounds.South,
                Direction.West => Bounds.West,
                _ => throw new InvalidOperationException()
            };

            return boundsForDir.Contains(modLocalPos);
        }

        [DataDefinition]
        public sealed partial class DirBoundData
        {
            [DataField("all")] public Box2 All;
            [DataField("north")] public Box2 North;
            [DataField("south")] public Box2 South;
            [DataField("east")] public Box2 East;
            [DataField("west")] public Box2 West;
        }
    }
}
