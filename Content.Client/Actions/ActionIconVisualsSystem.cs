using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Actions;

public sealed partial class ActionIconVisualsSystem : VisualizerSystem<ActionComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ActionComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<SpriteSpecifier>(uid, ActionState.DynamicIcon, out var icon, args.Component))
        {
            if (icon is SpriteSpecifier.EntityPrototype)
                SpriteSystem.LayerSetTexture((uid, args.Sprite), ActionVisuals.Icon, SpriteSystem.Frame0(icon));
            else
                SpriteSystem.LayerSetSprite((uid, args.Sprite), ActionVisuals.Icon, icon);
        }

        if (AppearanceSystem.TryGetData<Color>(uid, ActionState.Color, out var color, args.Component))
        {
            SpriteSystem.LayerSetColor((uid, args.Sprite), ActionVisuals.Icon, color);

            if (SpriteSystem.LayerExists((uid, args.Sprite), ActionVisuals.IconToggled))
                SpriteSystem.LayerSetColor((uid, args.Sprite), ActionVisuals.IconToggled, color);
        }
    }
}
