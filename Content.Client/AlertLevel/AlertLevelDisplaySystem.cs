using System.Linq;
using Content.Shared.AlertLevel;
using Robust.Client.GameObjects;

namespace Content.Client.AlertLevel;

public sealed class AlertLevelDisplaySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelDisplayComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, AlertLevelDisplayComponent alertLevelDisplay, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
        {
            return;
        }
        var layer = args.Sprite.LayerMapReserveBlank(AlertLevelDisplay.Layer);

        if (args.AppearanceData.TryGetValue(AlertLevelDisplay.Powered, out var poweredObject))
        {
            args.Sprite.LayerSetVisible(layer, poweredObject is true);
        }

        if (!args.AppearanceData.TryGetValue(AlertLevelDisplay.CurrentLevel, out var level))
        {
            args.Sprite.LayerSetState(layer, alertLevelDisplay.AlertVisuals.Values.First());
            return;
        }

        if (alertLevelDisplay.AlertVisuals.TryGetValue((string) level, out var visual))
        {
            args.Sprite.LayerSetState(layer, visual);
        }
        else
        {
            args.Sprite.LayerSetState(layer, alertLevelDisplay.AlertVisuals.Values.First());
        }
    }
}
