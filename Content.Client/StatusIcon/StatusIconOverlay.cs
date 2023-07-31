using System.Numerics;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.StatusIcon;

public sealed class StatusIconOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly StatusIconSystem _statusIcon;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    internal StatusIconOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();
        _statusIcon = _entity.System<StatusIconSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        var eyeRot = args.Viewport.Eye?.Rotation ?? default;

        var xformQuery = _entity.GetEntityQuery<TransformComponent>();
        var scaleMatrix = Matrix3.CreateScale(new Vector2(1, 1));
        var rotationMatrix = Matrix3.CreateRotation(-eyeRot);

        var query = _entity.AllEntityQueryEnumerator<StatusIconComponent, SpriteComponent, TransformComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite, out var xform, out var meta))
        {
            if (xform.MapID != args.MapId)
                continue;

            var icons = _statusIcon.GetStatusIcons(uid, meta);
            if (icons.Count == 0)
                continue;

            var bounds = comp.Bounds ?? sprite.Bounds;

            var worldPos = _transform.GetWorldPosition(xform, xformQuery);

            if (!bounds.Translated(worldPos).Intersects(args.WorldAABB))
                continue;

            var worldMatrix = Matrix3.CreateTranslation(worldPos);
            Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
            Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);
            handle.SetTransform(matty);

            var count = 0;
            var accOffsetL = 0;
            var accOffsetR = 0;
            icons.Sort();

            foreach (var proto in icons)
            {
                var texture = _sprite.Frame0(proto.Icon);

                float yOffset;
                float xOffset;

                // the icons are ordered left to right, top to bottom.
                // extra icons that don't fit are just cut off.
                if (count % 2 == 0)
                {
                    if (accOffsetL + texture.Height > sprite.Bounds.Height * EyeManager.PixelsPerMeter)
                        break;
                    accOffsetL += texture.Height;
                    yOffset = (bounds.Height + sprite.Offset.Y) / 2f - (float) accOffsetL / EyeManager.PixelsPerMeter;
                    xOffset = -(bounds.Width + sprite.Offset.X) / 2f;
                }
                else
                {
                    if (accOffsetR + texture.Height > sprite.Bounds.Height * EyeManager.PixelsPerMeter)
                        break;
                    accOffsetR += texture.Height;
                    yOffset = (bounds.Height + sprite.Offset.Y) / 2f - (float) accOffsetR / EyeManager.PixelsPerMeter;
                    xOffset = (bounds.Width + sprite.Offset.X) / 2f - (float) texture.Width / EyeManager.PixelsPerMeter;
                }
                count++;

                var position = new Vector2(xOffset, yOffset);
                handle.DrawTexture(texture, position);
            }
        }
    }
}
