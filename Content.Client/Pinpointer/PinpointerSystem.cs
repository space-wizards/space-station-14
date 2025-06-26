using System.Numerics;
using Content.Shared.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // we want to show pinpointers arrow direction relative
        // to players eye rotation (like it was in SS13)

        // because eye can change it rotation anytime
        // we need to update this arrow in a update loop
        var query = EntityQueryEnumerator<PinpointerComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var pinpointer, out var sprite))
        {
            if (!pinpointer.HasTarget)
                continue;
            var eye = _eyeManager.CurrentEye;
            var angle = pinpointer.ArrowAngle + eye.Rotation;

            if (!_sprite.TryGetLayer((uid, sprite), PinpointerLayers.Screen, out var layer, true))
                return;

            const float halfPixel = 0.5f / EyeManager.PixelsPerMeter;

            switch (pinpointer.DistanceToTarget)
            {
                case Distance.Close:
                case Distance.Medium:
                case Distance.Far:
                    // The point that the screen should be rotated is in the middle of a pixel
                    // We need to translate half a pixel, rotate, then translate back.
                    var translation = new Vector2(halfPixel, -halfPixel);
                    layer.LocalMatrix = Matrix3x2.CreateTranslation(translation) *
                                        Matrix3x2.CreateRotation((float)angle.Theta) *
                                        Matrix3x2.CreateTranslation(-translation);
                    break;
                default:
                    _sprite.LayerSetRotation((uid, sprite), PinpointerLayers.Screen, Angle.Zero);
                    break;
            }
        }
    }
}
