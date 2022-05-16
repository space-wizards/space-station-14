using Content.Shared.Chat.TypingIndicator;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Chat.TypingIndicator;

public sealed class TypingIndicatorVisualizerSystem : VisualizerSystem<TypingIndicatorComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TypingIndicatorComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, TypingIndicatorComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        var layer = sprite.LayerMapReserveBlank(TypingIndicatorLayers.Base);
        sprite.LayerSetRSI(layer, "Effects/speech.rsi");
        sprite.LayerSetState(layer, "default0");
        sprite.LayerSetVisible(layer, false);
        sprite.LayerSetShader(layer, "unshaded");
        sprite.LayerSetOffset(layer, new Vector2(0.5f, 0.5f));
    }

    protected override void OnAppearanceChange(EntityUid uid, TypingIndicatorComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        args.Component.TryGetData(TypingIndicatorVisuals.IsTyping, out bool isTyping);
        sprite.LayerSetVisible(TypingIndicatorLayers.Base, isTyping);
    }
}
