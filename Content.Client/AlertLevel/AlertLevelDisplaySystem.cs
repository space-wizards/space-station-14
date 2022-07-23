using System.Linq;
using Content.Shared.AlertLevel;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.AlertLevel;

public sealed class AlertLevelDisplaySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelDisplayComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, AlertLevelDisplayComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
        {
            return;
        }

        if (!args.Sprite.LayerMapTryGet(AlertLevelDisplay.Layer, out _))
        {
            var layer = args.Sprite.AddLayer(new RSI.StateId(component.AlertVisuals.Values.First()));
            args.Sprite.LayerMapSet(AlertLevelDisplay.Layer, layer);
        }

        if (!args.AppearanceData.TryGetValue(AlertLevelDisplay.CurrentLevel, out var level))
        {
            args.Sprite.LayerSetState(AlertLevelDisplay.Layer, new RSI.StateId(component.AlertVisuals.Values.First()));
            return;
        }

        if (component.AlertVisuals.TryGetValue((string) level, out var visual))
        {
            args.Sprite.LayerSetState(AlertLevelDisplay.Layer, new RSI.StateId(visual));
        }
        else
        {
            args.Sprite.LayerSetState(AlertLevelDisplay.Layer, new RSI.StateId(component.AlertVisuals.Values.First()));
        }
    }
}
