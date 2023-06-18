// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Photocopier;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Photocopier;

public sealed class PhotocopierSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    private readonly PhotocopierCombinedVisualState _fallbackVisualState =
        new(PhotocopierVisualState.Off, false, false);

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotocopierComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<PhotocopierComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnAppearanceChange(EntityUid uid, PhotocopierComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(PhotocopierVisuals.VisualState, out var visualStateObject) ||
            visualStateObject is not PhotocopierCombinedVisualState visualState)
        {
            visualState = _fallbackVisualState;
        }

        UpdateAppearance(uid, visualState, component, args.Sprite);
    }

    private void OnAnimationCompleted(EntityUid uid, PhotocopierComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !_appearanceSystem.TryGetData<PhotocopierCombinedVisualState>(uid, PhotocopierVisuals.VisualState, out var visualState, appearance))
        {
            visualState = _fallbackVisualState;
        }

        UpdateAppearance(uid, visualState, component, sprite);
    }

    private static void UpdateAppearance(EntityUid uid, PhotocopierCombinedVisualState visualState, PhotocopierComponent component, SpriteComponent sprite)
    {
        SetLayerState(PhotocopierVisualLayers.Base, "off", sprite);

        switch (visualState.State)
        {
            case PhotocopierVisualState.Off:
                HideLayer(PhotocopierVisualLayers.Led, sprite);
                HideLayer(PhotocopierVisualLayers.Top, sprite);
                HideLayer(PhotocopierVisualLayers.PrintAnim, sprite);
                break;

            case PhotocopierVisualState.Powered:
                SetLayerState(PhotocopierVisualLayers.Led, "led_powered", sprite);
                SetLayerState(PhotocopierVisualLayers.Top, "top_powered", sprite);
                HideLayer(PhotocopierVisualLayers.PrintAnim, sprite);
                break;

            case PhotocopierVisualState.OutOfToner:
                SetLayerState(PhotocopierVisualLayers.Led, "led_out", sprite);
                SetLayerState(PhotocopierVisualLayers.Top, "top_powered", sprite);
                HideLayer(PhotocopierVisualLayers.PrintAnim, sprite);
                break;

            case PhotocopierVisualState.Printing:
                SetLayerState(PhotocopierVisualLayers.Led, "led_printing", sprite);
                SetLayerState(PhotocopierVisualLayers.Top, "top_powered", sprite);
                SetLayerState(PhotocopierVisualLayers.PrintAnim, "printing_paper", sprite);
                break;

            case PhotocopierVisualState.Copying:
                SetLayerState(PhotocopierVisualLayers.Led, "led_printing", sprite);
                SetLayerState(
                    PhotocopierVisualLayers.Top,
                    visualState.Emagged ? "top_scanning_emagged" : "top_scanning",
                    sprite);
                SetLayerState(PhotocopierVisualLayers.PrintAnim, "printing_paper", sprite);
                break;
        }

        if (visualState.GotItem)
            SetLayerState(PhotocopierVisualLayers.TopPaper, "top_paper", sprite);
        else
            HideLayer(PhotocopierVisualLayers.TopPaper, sprite);
    }

    private static void SetLayerState(PhotocopierVisualLayers layer, string? state, SpriteComponent sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        sprite.LayerSetVisible(layer, true);
        sprite.LayerSetAutoAnimated(layer, true);
        sprite.LayerSetState(layer, state);
    }

    private static void HideLayer(PhotocopierVisualLayers layer, SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(layer, out var actualLayer))
            return;

        sprite.LayerSetVisible(actualLayer, false);
    }
}
