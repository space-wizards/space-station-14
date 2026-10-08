using System.Linq;
using Content.Shared.AlertLevel;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.AlertLevel;

public sealed partial class AlertLevelDisplaySystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(EntityUid uid, AlertLevelDisplayComponent alertLevelDisplay, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var layer = _sprite.LayerMapReserve((uid, args.Sprite), AlertLevelDisplay.Layer);

        if (args.TryGetData<bool>(AlertLevelDisplay.Powered, out var powered))
            _sprite.LayerSetVisible((uid, args.Sprite), layer, powered);

        if (!args.TryGetData<ProtoId<AlertLevelPrototype>>(AlertLevelDisplay.CurrentLevel, out var level))
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), layer, alertLevelDisplay.AlertVisuals.Values.First());
            return;
        }

        _sprite.LayerSetRsiState((uid, args.Sprite),
            layer,
            alertLevelDisplay.AlertVisuals.GetValueOrDefault(level) ?? alertLevelDisplay.AlertVisuals.Values.First());
    }
}
