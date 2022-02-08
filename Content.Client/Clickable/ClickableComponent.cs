using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using TerraFX.Interop.Windows;

namespace Content.Client.Clickable
{
    [RegisterComponent]
    public sealed class ClickableComponent : Component
    {
        [Dependency] private readonly IClickMapManager _clickMapManager = default!;

        [ViewVariables] [DataField("bounds")] private DirBoundData? _data;

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
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(Owner, out ISpriteComponent? sprite) || !sprite.Visible)
            {
                drawDepth = default;
                renderOrder = default;
                return false;
            }

            var transform = entMan.GetComponent<TransformComponent>(Owner);
            var localPos = transform.InvWorldMatrix.Transform(worldPos);
            var spriteMatrix = Matrix3.Invert(sprite.GetLocalMatrix());

            localPos = spriteMatrix.Transform(localPos);

            var found = false;
            var worldRotation = transform.WorldRotation;

            if (_data != null)
            {
                if (_data.All.Contains(localPos))
                {
                    found = true;
                }
                else
                {
                    // TODO: diagonal support?

                    var modAngle = sprite.NoRotation
                        ? SpriteComponent.CalcRectWorldAngle(worldRotation, 4)
                        : Angle.Zero;
                    var dir = sprite.EnableDirectionOverride
                        ? sprite.DirectionOverride
                        : worldRotation.GetCardinalDir();

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

                    var localOffset = layerPos * EyeManager.PixelsPerMeter * (1, -1);
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
