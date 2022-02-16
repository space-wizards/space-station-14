using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.Clickable
{
    [RegisterComponent]
    public sealed class ClickableComponent : Component
    {
        [Dependency] private readonly IClickMapManager _clickMapManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables] [DataField("bounds")] public DirBoundData? Bounds;

        /// <summary>
        /// Used to check whether a click worked. Will first check if the click falls inside of some explicit bounding
        /// boxes (see <see cref="Bounds"/>). If that fails, attempts to use automatically generated click maps.
        /// </summary>
        /// <param name="worldPos">The world position that was clicked.</param>
        /// <param name="drawDepth">
        /// The draw depth for the sprite that captured the click.
        /// </param>
        /// <returns>True if the click worked, false otherwise.</returns>
        public bool CheckClick(Vector2 worldPos, out int drawDepth, out uint renderOrder)
        {
            if (!_entMan.TryGetComponent(Owner, out ISpriteComponent? sprite) || !sprite.Visible)
            {
                drawDepth = default;
                renderOrder = default;
                return false;
            }

            drawDepth = sprite.DrawDepth;
            renderOrder = sprite.RenderOrder;

            var transform = _entMan.GetComponent<TransformComponent>(Owner);
            var worldRot = transform.WorldRotation;

            // We need to convert world angle to a positive value. between 0 and 2pi. This is important for
            // CalcRectWorldAngle to get the right angle. Otherwise can get incorrect results for sprites at angles like
            // -135 degrees (seems highly specific, but AI actors & other entities can snap to those angles while
            // moving). As to why we treat world-angle like this, but not eye angle or world+eye, it is just because
            // thats what sprite-rendering does.
            worldRot = worldRot.Reduced();

            if (worldRot.Theta < 0)
                worldRot = new Angle(worldRot.Theta + Math.Tau);

            var invSpriteMatrix = Matrix3.Invert(sprite.GetLocalMatrix());

            // This should have been the rotation of the sprite relative to the screen, but this is not the case with no-rot or directional sprites.
            var relativeRotation = worldRot + _eyeManager.CurrentEye.Rotation;

            // First we get `localPos`, the clicked location in the sprite-coordinate frame.
            var entityXform = Matrix3.CreateInverseTransform(transform.WorldPosition, sprite.NoRotation ? -_eyeManager.CurrentEye.Rotation : worldRot);
            var localPos = invSpriteMatrix.Transform(entityXform.Transform(worldPos));

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
                    var imagePos = (Vector2i) (localPos * EyeManager.PixelsPerMeter * (1, -1) + layer.Texture.Size / 2f);

                    if (_clickMapManager.IsOccluding(layer.Texture, imagePos))
                        return true;
                }

                // Either we weren't clicking on the texture, or there wasn't one. In which case: check the RSI next
                if (layer.State == null || layer.ActualRsi is not RSI rsi || !rsi.TryGetState(layer.State, out var rsiState))
                    continue;

                var dir = (rsiState.Directions == RSI.State.DirectionType.Dir1)
                    ? RSI.State.Direction.South
                    : relativeRotation.ToRsiDirection(rsiState.Directions);

                // convert to layer-local coordinates
                layer.GetLayerDrawMatrix(dir, out var matrix);
                var inverseMatrix = Matrix3.Invert(matrix);
                var layerLocal = inverseMatrix.Transform(localPos);

                // Convert to image coordinates
                var layerImagePos = (Vector2i) (layerLocal * EyeManager.PixelsPerMeter * (1, -1) + rsiState.Size / 2f);

                // Next, to get the right click map we need the "direction" of this layer that is actually being used to draw the sprite on the screen.
                // This **can** differ from the dir defined before, but can also just be the same.
                if (sprite.EnableDirectionOverride)
                    dir = sprite.DirectionOverride.Convert(rsiState.Directions);;
                dir = dir.OffsetRsiDir(layer.DirOffset);

                if (_clickMapManager.IsOccluding(layer.ActualRsi!, layer.State, dir, layer.AnimationFrame, layerImagePos))
                    return true;
            }

            drawDepth = default;
            renderOrder = default;
            return false;
        }

        public bool CheckDirBound(ISpriteComponent sprite, Angle relativeRotation, Vector2 localPos)
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
        public sealed class DirBoundData
        {
            [ViewVariables] [DataField("all")] public Box2 All;
            [ViewVariables] [DataField("north")] public Box2 North;
            [ViewVariables] [DataField("south")] public Box2 South;
            [ViewVariables] [DataField("east")] public Box2 East;
            [ViewVariables] [DataField("west")] public Box2 West;
        }
    }
}
