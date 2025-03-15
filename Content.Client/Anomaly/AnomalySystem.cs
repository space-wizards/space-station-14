using System.Numerics;
using Content.Client.Gravity;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Anomaly;

public sealed class AnomalySystem : SharedAnomalySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly FloatingVisualizerSystem _floating = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        SubscribeLocalEvent<AnomalyComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AnomalyComponent, AnimationCompletedEvent>(OnAnimationComplete);

        SubscribeLocalEvent<AnomalySupercriticalComponent, ComponentShutdown>(OnShutdown);
    }
    private void OnStartup(EntityUid uid, AnomalyComponent component, ComponentStartup args)
    {
        _floating.FloatAnimation(uid, component.FloatingOffset, component.AnimationKey, component.AnimationTime);
    }

    private void OnAnimationComplete(EntityUid uid, AnomalyComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != component.AnimationKey)
            return;
        _floating.FloatAnimation(uid, component.FloatingOffset, component.AnimationKey, component.AnimationTime);
    }

    private void OnAppearanceChanged(EntityUid uid, AnomalyComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!Appearance.TryGetData<bool>(uid, AnomalyVisuals.IsPulsing, out var pulsing, args.Component))
            pulsing = false;

        if (Appearance.TryGetData<bool>(uid, AnomalyVisuals.Supercritical, out var super, args.Component) && super)
            pulsing = super;

        if (HasComp<AnomalySupercriticalComponent>(uid))
            pulsing = true;

        if (!sprite.LayerMapTryGet(AnomalyVisualLayers.Base, out var layer) ||
            !sprite.LayerMapTryGet(AnomalyVisualLayers.Animated, out var animatedLayer))
            return;

        sprite.LayerSetVisible(layer, !pulsing);
        sprite.LayerSetVisible(animatedLayer, pulsing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnomalySupercriticalComponent, SpriteComponent>();

        while (query.MoveNext(out var super, out var sprite))
        {
            var completion = 1f - (float) ((super.EndTime - _timing.CurTime) / super.SupercriticalDuration);
            var scale = completion * (super.MaxScaleAmount - 1f) + 1f;
            sprite.Scale = new Vector2(scale, scale);

            var transparency = (byte) (65 * (1f - completion) + 190);
            if (transparency < sprite.Color.AByte)
            {
                sprite.Color = sprite.Color.WithAlpha(transparency);
            }
        }
    }

    private void OnShutdown(Entity<AnomalySupercriticalComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        sprite.Scale = Vector2.One;
        sprite.Color = sprite.Color.WithAlpha(1f);
    }
}
