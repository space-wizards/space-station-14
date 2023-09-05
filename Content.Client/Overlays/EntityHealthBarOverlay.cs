using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using System.Numerics;
using static Robust.Shared.Maths.Color;

namespace Content.Client.Overlays;

/// <summary>
/// Overlay that shows a health bar on mobs.
/// </summary>
public sealed class EntityHealthBarOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly SharedTransformSystem _transform;
    private readonly MobStateSystem _mobStateSystem;
    private readonly MobThresholdSystem _mobThresholdSystem;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public HashSet<string> DamageContainers = new();

    public EntityHealthBarOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _mobStateSystem = _entManager.EntitySysManager.GetEntitySystem<MobStateSystem>();
        _mobThresholdSystem = _entManager.EntitySysManager.GetEntitySystem<MobThresholdSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        const float scale = 1f;
        var scaleMatrix = Matrix3.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3.CreateRotation(-rotation);

        var query = _entManager.AllEntityQueryEnumerator<MobThresholdsComponent, MobStateComponent, DamageableComponent, SpriteComponent>();
        while (query.MoveNext(out var uid,
            out var mobThresholdsComponent,
            out var mobStateComponent,
            out var damageableComponent,
            out var spriteComponent))
        {
            if (_entManager.TryGetComponent<MetaDataComponent>(uid, out var metaDataComponent) &&
                metaDataComponent.Flags.HasFlag(MetaDataFlags.InContainer))
            {
                continue;
            }

            if (!xformQuery.TryGetComponent(uid, out var xform) ||
                xform.MapID != args.MapId)
            {
                continue;
            }

            if (damageableComponent.DamageContainerID == null || !DamageContainers.Contains(damageableComponent.DamageContainerID))
            {
                continue;
            }

            var bounds = spriteComponent.Bounds;
            var worldPos = _transform.GetWorldPosition(xform, xformQuery);

            if (!bounds.Translated(worldPos).Intersects(args.WorldAABB))
            {
                continue;
            }

            var worldPosition = _transform.GetWorldPosition(xform);
            var worldMatrix = Matrix3.CreateTranslation(worldPosition);

            Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
            Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);

            handle.SetTransform(matty);

            var yOffset = spriteComponent.Bounds.Height * EyeManager.PixelsPerMeter / 2 - 3f;
            var widthOfMob = spriteComponent.Bounds.Width * EyeManager.PixelsPerMeter;

            var position = new Vector2(-widthOfMob / EyeManager.PixelsPerMeter / 2, yOffset / EyeManager.PixelsPerMeter);

            // we are all progressing towards death every day
            (float ratio, bool inCrit) deathProgress = CalcProgress(uid, mobStateComponent, damageableComponent, mobThresholdsComponent);

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

        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
    }

    /// <summary>
    /// Returns a ratio between 0 and 1, and whether the entity is in crit.
    /// </summary>
    private (float, bool) CalcProgress(EntityUid uid, MobStateComponent component, DamageableComponent dmg, MobThresholdsComponent thresholds)
    {
        if (_mobStateSystem.IsAlive(uid, component))
        {
            if (!_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var threshold, thresholds) &&
                !_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Dead, out threshold, thresholds))
                return (1, false);

            var ratio = 1 - ((FixedPoint2) (dmg.TotalDamage / threshold)).Float();
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

    public static Color GetProgressColor(float progress, bool crit)
    {
        if (progress >= 1.0f)
        {
            return SeaBlue;
        }

        if (!crit)
        {
            switch (progress)
            {
                case > 0.90F:
                    return SeaBlue;
                case > 0.50F:
                    return Violet;
                case > 0.15F:
                    return Ruber;
            }
        }

        return VividGamboge;
    }
}
