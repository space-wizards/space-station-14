using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.Clickable
{
    [RegisterComponent]
    public sealed class ClickableComponent : Component
    {
        public override string Name => "Clickable";

        [Dependency] private readonly IClickMapManager _clickMapManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables] [DataField("bounds")] private DirBoundData? _data;

        /// <summary>
        /// Used to check whether a click worked. Will first check if the click falls inside of some explicit bounding
        /// boxes (see <see cref="_data"/>). If that fails, attempts to use automatically generated click maps.
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
            var invSpriteMatrix = Matrix3.CreateTransform(Vector2.Zero, -sprite.Rotation, (1,1)/sprite.Scale);
            var relativeRotation = worldRot + _eyeManager.CurrentEye.Rotation;

            // localPos would be the clicked location in the entity's coordinate frame if it weren't for sprite.Offset.
            var localPos = transform.InvWorldMatrix.Transform(worldPos) - sprite.Offset;

            // Check explicitly defined click-able bounds
            if (CheckDirBound(sprite, relativeRotation, localPos, invSpriteMatrix))
                return true;

            // Next check each individual sprite layer using automatically computed click maps.
            foreach (var spriteLayer in sprite.AllLayers)
            {
                if (!spriteLayer.Visible || spriteLayer is not Layer layer)
                    continue;

                // How many orientations does this rsi have?
                var dirCount = sprite.GetLayerDirectionCount(layer);

                // If the sprite does not actually rotate we need to fix the rotation that was added to localPos via InvWorldMatrix
                var modAngle = Angle.Zero;
                if (sprite.NoRotation)
                    modAngle += CalcRectWorldAngle(relativeRotation, dirCount);

                // Check the layer's texture, if it has one
                if (layer.Texture != null)
                {
                    // Convert to sprite-coordinates. This includes sprite matrix (scale, rotation), and noRot corrections (modAngle)
                    var spritePos = invSpriteMatrix.Transform(modAngle.RotateVec(localPos));

                    // Convert to image coordinates
                    var imagePos = (Vector2i) (spritePos * EyeManager.PixelsPerMeter * (1, -1) + layer.Texture.Size / 2f);

                    if (_clickMapManager.IsOccluding(layer.Texture, imagePos))
                        return true;
                }

                // As the texture failed, check the RSI next
                if (layer.State == null || layer.ActualRsi is not RSI rsi || !rsi.TryGetState(layer.State, out var state))
                    continue;

                // Get the sprite direction, in the absence of any direction offset or override. this is just for
                // transforming the sprite position.
                var dir = relativeRotation.ToRsiDirection(state.Directions);

                // Yes, angle -> dir -> angle is a bit silly, and there's probably a more direct calculation, but state.Directions could be one of three things and
                // I CBF doing it manually in a case switch or something.
                modAngle += dir.Convert().ToAngle();

                // TODO SPRITE LAYER ROTATION
                // Currently this doesn't support layers with scale & rotation. Whenever/if-ever that should be added,
                // this needs fixing. See also Layer.CalculateBoundingBox and other engine code with similar warnings.

                // Convert to sprite-coordinates. This is the same as for the texture check, but now also includes directional angle-snapping corrections in modAngle
                var layerPos = invSpriteMatrix.Transform(modAngle.RotateVec(localPos));

                // Convert to image coordinates
                var layerImagePos = (Vector2i) (layerPos * EyeManager.PixelsPerMeter * (1, -1) + layer.ActualRsi.Size / 2f);

                // Next, to get the right click map we need the "direction" of this layer that is actually being used to draw the sprite on the screen.
                // This differs from the dir above which is just for coordinate transformations.
                if (sprite.EnableDirectionOverride)
                {
                    dir = sprite.DirectionOverride.Convert(state.Directions);
                    dir = dir.OffsetRsiDir(layer.DirOffset);
                }
                else
                    dir = dir.OffsetRsiDir(layer.DirOffset);

                if (_clickMapManager.IsOccluding(layer.ActualRsi, layer.State, dir, layer.AnimationFrame, layerImagePos))
                    return true;
            }

            drawDepth = default;
            renderOrder = default;
            return false;
        }

        public bool CheckDirBound(ISpriteComponent sprite, Angle relativeRotation, Vector2 localPos, Matrix3 spriteMatrix)
        {
            if (_data == null)
                return false;

            // Here we get sprite orientation, either from an explicit override or just the relative rotation
            var direction = relativeRotation.GetCardinalDir();

            // Assuming the sprite snaps to 4 orientations we need to adjust our localPos relative to the entity by 90
            // degree steps. Effectively, this accounts for the fact that the entity's world rotation + sprite rotation
            // does not match the **actual** drawn rotation.
            var modAngle = direction.ToAngle();

            // If our sprite does not rotate at all, we shouldn't have been bothering with all that rotation logic,
            // but it sorta came for free with the matrix transform, but we gotta undo that now.
            if (sprite.NoRotation)
                modAngle += CalcRectWorldAngle(relativeRotation, 4);

            var spritePos = spriteMatrix.Transform(modAngle.RotateVec(localPos));

            // First, check the bounding box from DirBoundData _data that this is valid for all orientations
            if (_data.All.Contains(spritePos))
                return true;

            // otherwise, get and check the appropriate bounding box for the current sprite orientation
            var boundsForDir = (sprite.EnableDirectionOverride ? sprite.DirectionOverride : direction) switch
            {
                Direction.East => _data.East,
                Direction.North => _data.North,
                Direction.South => _data.South,
                Direction.West => _data.West,
                _ => throw new InvalidOperationException()
            };

            // and check if our adjusted point lies inside of it
            return boundsForDir.Contains(spritePos);
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
