using Content.Client.Resources;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.DoAfter;

public sealed class DoAfterOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly SharedTransformSystem _transform;

    private readonly Texture _barTexture;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public DoAfterOverlay(IEntityManager entManager, IPrototypeManager protoManager, IResourceCache cache)
    {
        _entManager = entManager;
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _barTexture = cache.GetTexture("/Textures/Interface/Misc/progress_bar.rsi/icon.png");

        _shader = protoManager.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var spriteQuery = _entManager.GetEntityQuery<SpriteComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        // If you use the display UI scale then need to set max(1f, displayscale) because 0 is valid.
        const float scale = 1f;
        var scaleMatrix = Matrix3.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3.CreateRotation(-rotation);
        handle.UseShader(_shader);

        // TODO: Need active DoAfter component (or alternatively just make DoAfter itself active)
        foreach (var comp in _entManager.EntityQuery<DoAfterComponent>(true))
        {
            if (comp.DoAfters.Count == 0 ||
                !xformQuery.TryGetComponent(comp.Owner, out var xform) ||
                xform.MapID != args.MapId)
            {
                continue;
            }

            var worldPosition = _transform.GetWorldPosition(xform);
            var index = 0;
            var worldMatrix = Matrix3.CreateTranslation(worldPosition);

            foreach (var (_, doAfter) in comp.DoAfters)
            {
                var elapsed = doAfter.Accumulator;
                var displayRatio = MathF.Min(1.0f,
                    elapsed / doAfter.Delay);

                Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
                Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);

                handle.SetTransform(matty);
                var offset = _barTexture.Height / scale * index;

                // Use the sprite itself if we know its bounds. This means short or tall sprites don't get overlapped
                // by the bar.
                float yOffset;
                if (spriteQuery.TryGetComponent(comp.Owner, out var sprite))
                {
                    yOffset = sprite.Bounds.Height / 2f + 0.05f;
                }
                else
                {
                    yOffset = 0.5f;
                }

                // Position above the entity (we've already applied the matrix transform to the entity itself)
                // Offset by the texture size for every do_after we have.
                var position = new Vector2(-_barTexture.Width / 2f / EyeManager.PixelsPerMeter,
                    yOffset / scale + offset / EyeManager.PixelsPerMeter * scale);

                // Draw the underlying bar texture
                handle.DrawTexture(_barTexture, position);

                // Draw the bar itself
                var cancelled = doAfter.Cancelled;
                Color color;
                const float flashTime = 0.125f;

                // if we're cancelled then flick red / off.
                if (cancelled)
                {
                    var flash = Math.Floor(doAfter.CancelledAccumulator / flashTime) % 2 == 0;
                    color = new Color(1f, 0f, 0f, flash ? 1f : 0f);
                }
                else
                {
                    color = GetProgressColor(displayRatio);
                }

                // Hardcoded width of the progress bar because it doesn't match the texture.
                const float startX = 2f;
                const float endX = 22f;

                var xProgress = (endX - startX) * displayRatio + startX;

                var box = new Box2(new Vector2(startX, 3f) / EyeManager.PixelsPerMeter, new Vector2(xProgress, 4f) / EyeManager.PixelsPerMeter);
                box = box.Translated(position);
                handle.DrawRect(box, color);

                index++;
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
    }

    public static Color GetProgressColor(float progress)
    {
        if (progress >= 1.0f)
        {
            return new Color(0f, 1f, 0f);
        }
        // lerp
        var hue = (5f / 18f) * progress;
        return Color.FromHsv((hue, 1f, 0.75f, 1f));
    }
}
