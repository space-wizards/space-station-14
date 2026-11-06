using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Actions;

public sealed partial class ActionIconVisualsSystem : VisualizerSystem<ActionComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, ActionComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.TryGetData<SpriteSpecifier>(ActionState.DynamicIcon, out var icon))
        {
            if (icon is SpriteSpecifier.EntityPrototype)
                SpriteSystem.LayerSetTexture((uid, args.Sprite), ActionVisuals.Icon, SpriteSystem.Frame0(icon));
            else
                SpriteSystem.LayerSetSprite((uid, args.Sprite), ActionVisuals.Icon, icon);
        }

        if (args.TryGetData<Color>(ActionState.Color, out var color))
        {
            SpriteSystem.LayerSetColor((uid, args.Sprite), ActionVisuals.Icon, color);

            if (SpriteSystem.LayerExists((uid, args.Sprite), ActionVisuals.IconToggled))
                SpriteSystem.LayerSetColor((uid, args.Sprite), ActionVisuals.IconToggled, color);
        }
    }
}
