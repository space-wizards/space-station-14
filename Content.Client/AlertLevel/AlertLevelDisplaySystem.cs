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

        SubscribeLocalEvent<AlertLevelDisplayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AlertLevelDisplayComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnInit(EntityUid uid, AlertLevelDisplayComponent component, ComponentInit args)
    {
         if (!EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
         {
             return;
         }

         var layer = sprite.AddLayer(new RSI.StateId(component.AlertVisuals.Values.First()));
         sprite.LayerMapSet(AlertLevelDisplay.Layer, layer);
    }

    private void OnAppearanceChange(EntityUid uid, AlertLevelDisplayComponent component, AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.TryGetValue(AlertLevelDisplay.CurrentLevel, out var level)
            || !EntityManager.TryGetComponent(component.Owner, out SpriteComponent? sprite))
        {
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
