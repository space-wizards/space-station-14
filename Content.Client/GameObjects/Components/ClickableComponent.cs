#nullable enable
using System;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public sealed class ClickableComponent : Component
    {
        public override string Name => "Clickable";

        [Dependency] private readonly IClickMapManager _clickMapManager = default!;

        [ViewVariables] private DirBoundData _data = default!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _data, "bounds", DirBoundData.Default);
        }

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

            var localPos = Owner.Transform.InvWorldMatrix.Transform(worldPos);

            var worldRotation = new Angle(Owner.Transform.WorldRotation - sprite.Rotation);
            if (sprite.Directional)
            {
                localPos = new Angle(worldRotation).RotateVec(localPos);
            }
            else
            {
                localPos = new Angle(MathHelper.PiOver2).RotateVec(localPos);
            }

            var localOffset = localPos * EyeManager.PixelsPerMeter * (1, -1);

            var found = false;

            if (_data.All.Contains(localPos))
            {
                found = true;
            }
            else
            {
                // TODO: diagonal support?
                var dir = sprite.Directional ? worldRotation.GetCardinalDir() : Direction.South;
                var boundsForDir = dir switch
                {
                    Direction.East => _data.East,
                    Direction.North => _data.North,
                    Direction.South => _data.South,
                    Direction.West => _data.West,
                    _ => throw new InvalidOperationException()
                };

                if (boundsForDir.Contains(localPos))
                {
                    found = true;
                }
            }

            if (!found)
            {
                foreach (var layer in sprite.AllLayers)
                {
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

                        var dir = layer.EffectiveDirection(worldRotation);
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

        private sealed class DirBoundData : IExposeData
        {
            [ViewVariables] public Box2 All;
            [ViewVariables] public Box2 North;
            [ViewVariables] public Box2 South;
            [ViewVariables] public Box2 East;
            [ViewVariables] public Box2 West;

            public static DirBoundData Default { get; } = new();

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref All, "all", default);
                serializer.DataField(ref North, "north", default);
                serializer.DataField(ref South, "south", default);
                serializer.DataField(ref East, "east", default);
                serializer.DataField(ref West, "west", default);
            }
        }
    }
}
