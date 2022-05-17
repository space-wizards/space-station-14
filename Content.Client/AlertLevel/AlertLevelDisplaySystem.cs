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
        if (!EntityManager.TryGetComponent(component.Owner, out SpriteComponent? sprite))
        {
            return;
        }

        if (!sprite.LayerMapTryGet(AlertLevelDisplay.Layer, out _))
        {
            var layer = sprite.AddLayer(new RSI.StateId(component.AlertVisuals.Values.First()));
            sprite.LayerMapSet(AlertLevelDisplay.Layer, layer);
        }

        if (!args.AppearanceData.TryGetValue(AlertLevelDisplay.CurrentLevel, out var level))
        {
            sprite.LayerSetState(AlertLevelDisplay.Layer, new RSI.StateId(component.AlertVisuals.Values.First()));
            return;
        }

        if (component.AlertVisuals.TryGetValue((string) level, out var visual))
        {
            sprite.LayerSetState(AlertLevelDisplay.Layer, new RSI.StateId(visual));
        }
        else
        {
            sprite.LayerSetState(AlertLevelDisplay.Layer, new RSI.StateId(component.AlertVisuals.Values.First()));
        }
    }
}
