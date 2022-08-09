using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.DoAfter;

public sealed class DoAfterOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly SharedTransformSystem _transform;

    private Texture _barTexture;
    private ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public DoAfterOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _barTexture = IoCManager.Resolve<IResourceCache>()
            .GetTexture("/Textures/Interface/Misc/progress_bar.rsi/icon.png");

        _shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        // TODO: This doesn't work well with packetloss.
        // Ideally we'd receive it from the server with its start time and duration
        // then we'd work out the diff between current time and start time, then we'd accumulate it
        // every first-time-predicted, then for the in between frames just render at the progress of the tick
        var currentTime = _timing.CurTime;

        // TODO: Get UI scale
        var scale = Vector2.One * 1f;
        var scaleMatrix = Matrix3.CreateScale(scale);
        var rotationMatrix = Matrix3.CreateRotation(-rotation);
        handle.UseShader(_shader);

        // TODO: Need active DoAfter component (or alternatively just make DoAfter itself active)
        foreach (var (comp, xform) in _entManager.EntityQuery<DoAfterComponent, TransformComponent>(true))
        {
            if (comp.DoAfters.Count == 0 ||
                xform.MapID != args.MapId)
            {
                continue;
            }

            var worldPosition = _transform.GetWorldPosition(xform);

            if (!args.WorldAABB.Contains(worldPosition))
                continue;

            var index = 0;
            var worldMatrix = Matrix3.CreateTranslation(worldPosition);

            foreach (var (_, doAfter) in comp.DoAfters)
            {
                var ratio = (currentTime - doAfter.StartTime).TotalSeconds;

                // Just in case it doesn't get cleaned up by the system for whatever reason.
                if (ratio > doAfter.Delay + DoAfterSystem.ExcessTime)
                    continue;

                var displayRatio = MathF.Min(1.0f,
                    (float) ratio / doAfter.Delay);

                Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
                Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);

                handle.SetTransform(matty);
                var offset = _barTexture.Height / scale.Y * index;
                var position = new Vector2(-_barTexture.Width / 2f / EyeManager.PixelsPerMeter,
                    0.5f / scale.Y + offset / EyeManager.PixelsPerMeter * scale.Y);

                // Draw the underlying bar texture
                handle.DrawTexture(_barTexture, position);

                // Draw the bar itself
                var cancelled = false;
                Color color;

                // TODO: Do cancellations
                if (cancelled)
                {
                    color = Color.White;
                }
                else
                {
                    color = GetProgressColor(displayRatio);
                }

                var startX = 2f;
                var endX = 22f;

                var xProgress = (endX - startX) * displayRatio + startX;

                // handle.UseShader(_shader);
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
