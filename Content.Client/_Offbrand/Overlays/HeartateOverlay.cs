using System.Numerics;
using Content.Client.StatusIcon;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.StatusIcon.Components;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Offbrand.Overlays;

public sealed class HeartrateOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly StatusIconSystem _statusIcon;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public ProtoId<HealthIconPrototype>? StatusIcon;

    private static readonly SpriteSpecifier HudStopped = new SpriteSpecifier.Rsi(new("/Textures/_Offbrand/heart_rate_hud.rsi"), "hud_stopped");
    private static readonly SpriteSpecifier HudGood = new SpriteSpecifier.Rsi(new("/Textures/_Offbrand/heart_rate_hud.rsi"), "hud_normal");
    private static readonly SpriteSpecifier HudOkay = new SpriteSpecifier.Rsi(new("/Textures/_Offbrand/heart_rate_hud.rsi"), "hud_okay");
    private static readonly SpriteSpecifier HudPoor = new SpriteSpecifier.Rsi(new("/Textures/_Offbrand/heart_rate_hud.rsi"), "hud_poor");
    private static readonly SpriteSpecifier HudBad = new SpriteSpecifier.Rsi(new("/Textures/_Offbrand/heart_rate_hud.rsi"), "hud_bad");
    private static readonly SpriteSpecifier HudDanger = new SpriteSpecifier.Rsi(new("/Textures/_Offbrand/heart_rate_hud.rsi"), "hud_danger");

    public HeartrateOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entityManager.System<SharedTransformSystem>();
        _sprite = _entityManager.System<SpriteSystem>();
        _statusIcon = _entityManager.System<StatusIconSystem>();
    }

    private SpriteSpecifier GetIcon(Entity<HeartrateComponent> ent)
    {
        var strain = ent.Comp.Strain;
        return strain.Double() switch {
            _ when !ent.Comp.Running => HudStopped,
            >= 4 => HudDanger,
            >= 3 => HudBad,
            >= 2 => HudPoor,
            >= 1 => HudOkay,
            _ => HudGood,
        };
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        const float scale = 1f;
        var scaleMatrix = Matrix3Helpers.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-rotation);

        _prototype.TryIndex(StatusIcon, out var statusIcon);

        var query = _entityManager.AllEntityQueryEnumerator<MetaDataComponent, TransformComponent, HeartrateComponent, SpriteComponent>();
        while (query.MoveNext(out var uid,
            out var metadata,
            out var xform,
            out var heartrate,
            out var sprite))
        {
            if (statusIcon != null && !_statusIcon.IsVisible((uid, metadata), statusIcon))
                continue;

            var bounds = _entityManager.GetComponentOrNull<StatusIconComponent>(uid)?.Bounds ?? _sprite.GetLocalBounds((uid, sprite));
            var worldPos = _transform.GetWorldPosition(xform);

            if (!bounds.Translated(worldPos).Intersects(args.WorldAABB))
                continue;

            var worldPosition = _transform.GetWorldPosition(xform);
            var worldMatrix = Matrix3Helpers.CreateTranslation(worldPosition);

            var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
            var matty = Matrix3x2.Multiply(rotationMatrix, scaledWorld);

            handle.SetTransform(matty);

            var curTime = _timing.RealTime;
            var texture = _sprite.GetFrame(GetIcon((uid, heartrate)), curTime);

            handle.DrawTexture(texture, new Vector2(-8f, 8f) / EyeManager.PixelsPerMeter);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
