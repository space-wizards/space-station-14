using System.Numerics;
using Content.Client.StatusIcon;
using Content.Client.UserInterface.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using static Robust.Shared.Maths.Color;

namespace Content.Client.Overlays;

/// <summary>
/// Overlay that shows a health bar on mobs.
/// </summary>
public sealed class EntityHealthBarOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _prototype;

    private readonly SharedTransformSystem _transform;
    private readonly MobStateSystem _mobStateSystem;
    private readonly MobThresholdSystem _mobThresholdSystem;
    private readonly StatusIconSystem _statusIconSystem;
    private readonly SpriteSystem _spriteSystem;
    private readonly ProgressColorSystem _progressColor;


    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public HashSet<string> DamageContainers = new();
    public ProtoId<HealthIconPrototype>? StatusIcon;

    public EntityHealthBarOverlay(IEntityManager entManager, IPrototypeManager prototype)
    {
        _entManager = entManager;
        _prototype = prototype;
        _transform = _entManager.System<SharedTransformSystem>();
        _mobStateSystem = _entManager.System<MobStateSystem>();
        _mobThresholdSystem = _entManager.System<MobThresholdSystem>();
        _statusIconSystem = _entManager.System<StatusIconSystem>();
        _spriteSystem = _entManager.System<SpriteSystem>();
        _progressColor = _entManager.System<ProgressColorSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        const float scale = 1f;
        var scaleMatrix = Matrix3Helpers.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-rotation);
        _prototype.TryIndex(StatusIcon, out var statusIcon);

        var query = _entManager.AllEntityQueryEnumerator<MobThresholdsComponent, MobStateComponent, DamageableComponent, SpriteComponent>();
        while (query.MoveNext(out var uid,
            out var mobThresholdsComponent,
            out var mobStateComponent,
            out var damageableComponent,
            out var spriteComponent))
        {
            if (statusIcon != null && !_statusIconSystem.IsVisible((uid, _entManager.GetComponent<MetaDataComponent>(uid)), statusIcon))
                continue;

            // We want the stealth user to still be able to see his health bar himself
            if (!xformQuery.TryGetComponent(uid, out var xform) ||
                xform.MapID != args.MapId)
                continue;

            if (damageableComponent.DamageContainerID == null || !DamageContainers.Contains(damageableComponent.DamageContainerID))
                continue;

            // we use the status icon component bounds if specified otherwise use sprite
            var bounds = _entManager.GetComponentOrNull<StatusIconComponent>(uid)?.Bounds ?? _spriteSystem.GetLocalBounds((uid, spriteComponent));
            var worldPos = _transform.GetWorldPosition(xform, xformQuery);

            if (!bounds.Translated(worldPos).Intersects(args.WorldAABB))
                continue;

            // we are all progressing towards death every day
            if (CalcProgress(uid, mobStateComponent, damageableComponent, mobThresholdsComponent) is not { } deathProgress)
                continue;

            var worldPosition = _transform.GetWorldPosition(xform);
            var worldMatrix = Matrix3Helpers.CreateTranslation(worldPosition);

            var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
            var matty = Matrix3x2.Multiply(rotationMatrix, scaledWorld);

            handle.SetTransform(matty);

            var yOffset = bounds.Height * EyeManager.PixelsPerMeter / 2 - 3f;
            var widthOfMob = bounds.Width * EyeManager.PixelsPerMeter;

            var position = new Vector2(-widthOfMob / EyeManager.PixelsPerMeter / 2, yOffset / EyeManager.PixelsPerMeter);
            var color = GetProgressColor(deathProgress.ratio, deathProgress.inCrit);

            // Hardcoded width of the progress bar because it doesn't match the texture.
            const float startX = 8f;
            var endX = widthOfMob - 8f;

            var xProgress = (endX - startX) * deathProgress.ratio + startX;

            var boxBackground = new Box2(new Vector2(startX, 0f) / EyeManager.PixelsPerMeter, new Vector2(endX, 3f) / EyeManager.PixelsPerMeter);
            boxBackground = boxBackground.Translated(position);
            handle.DrawRect(boxBackground, Black.WithAlpha(192));

            var boxMain = new Box2(new Vector2(startX, 0f) / EyeManager.PixelsPerMeter, new Vector2(xProgress, 3f) / EyeManager.PixelsPerMeter);
            boxMain = boxMain.Translated(position);
            handle.DrawRect(boxMain, color);

            var pixelDarken = new Box2(new Vector2(startX, 2f) / EyeManager.PixelsPerMeter, new Vector2(xProgress, 3f) / EyeManager.PixelsPerMeter);
            pixelDarken = pixelDarken.Translated(position);
            handle.DrawRect(pixelDarken, Black.WithAlpha(128));
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    /// <summary>
    /// Returns a ratio between 0 and 1, and whether the entity is in crit.
    /// </summary>
    private (float ratio, bool inCrit)? CalcProgress(EntityUid uid, MobStateComponent component, DamageableComponent dmg, MobThresholdsComponent thresholds)
    {
        if (_mobStateSystem.IsAlive(uid, component))
        {
            if (dmg.HealthBarThreshold != null && dmg.TotalDamage < dmg.HealthBarThreshold)
                return null;

            if (!_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var threshold, thresholds) &&
                !_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Dead, out threshold, thresholds))
                return (1, false);

            var ratio = 1 - ((FixedPoint2)(dmg.TotalDamage / threshold)).Float();
            return (ratio, false);
        }

        if (_mobStateSystem.IsCritical(uid, component))
        {
            if (!_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var critThreshold, thresholds) ||
                !_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold, thresholds))
            {
                return (1, true);
            }

            var ratio = 1 - ((dmg.TotalDamage - critThreshold) / (deadThreshold - critThreshold)).Value.Float();

            return (ratio, true);
        }

        return (0, true);
    }

    public Color GetProgressColor(float progress, bool crit)
    {
        if (crit)
            progress = 0;

        return _progressColor.GetProgressColor(progress);
    }
}
