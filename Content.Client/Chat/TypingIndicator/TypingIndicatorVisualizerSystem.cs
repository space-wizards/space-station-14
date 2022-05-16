using Content.Shared.Chat.TypingIndicator;
using Robust.Client.GameObjects;

namespace Content.Client.Chat.TypingIndicator;

public sealed class TypingIndicatorVisualizerSystem : VisualizerSystem<TypingIndicatorComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, TypingIndicatorComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        args.Component.TryGetData(TypingIndicatorVisuals.IsTyping, out bool isTyping);
        sprite.Color = isTyping ? Color.Green : Color.Red;

    }
}
