using System.Numerics;
using Content.Shared.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PinpointerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<PinpointerComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is null)
            return;

        UpdateArrow((ent, ent.Comp, args.Sprite), force: true);
    }

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
            UpdateArrow((uid, pinpointer, sprite));
        }
    }

    private void UpdateArrow(Entity<PinpointerComponent, SpriteComponent> ent, bool force = false)
    {
        if (!ent.Comp1.HasTarget)
            return;
        var eye = _eyeManager.CurrentEye;
        var angle = ent.Comp1.ArrowAngle + eye.Rotation;

        if (angle == ent.Comp1.CurrentRenderedAngle && !force)
            return;

        if (!_sprite.TryGetLayer((ent, ent.Comp2), PinpointerLayers.Screen, out var layer, true))
            return;

        const float halfPixel = 0.5f / EyeManager.PixelsPerMeter;

        Angle? newAngle = ent.Comp1.DistanceToTarget switch
        {
            Distance.Close or Distance.Medium or Distance.Far => angle,
            _ => null,
        };

        // The point that the screen should be rotated is in the middle of a pixel
        // We need to translate half a pixel, rotate, then translate back.
        var translation = new Vector2(halfPixel, -halfPixel);
        layer.LocalMatrix = Matrix3x2.CreateTranslation(translation) *
                            Matrix3x2.CreateRotation((float)(newAngle?.Theta ?? 0.0f)) *
                            Matrix3x2.CreateTranslation(-translation);

        ent.Comp1.CurrentRenderedAngle = newAngle;
    }
}
