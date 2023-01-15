using Content.Shared.Anomaly;
using Robust.Client.GameObjects;

namespace Content.Client.Anomaly;

public sealed class AnomalySystem : SharedAnomalySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, AnomalyComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!_appearance.TryGetData(uid, AnomalyVisuals.IsPulsing, out bool pulsing, args.Component))
            pulsing = false;

        if (_appearance.TryGetData(uid, AnomalyVisuals.IsPulsing, out bool super, args.Component) && super)
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
    }
}
