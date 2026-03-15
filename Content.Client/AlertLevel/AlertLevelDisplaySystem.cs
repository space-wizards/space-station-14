using System.Linq;
using Content.Shared.AlertLevel;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.AlertLevel;

public sealed class AlertLevelDisplaySystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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
        var layer = _sprite.LayerMapReserve((uid, args.Sprite), AlertLevelDisplay.Layer);

        if (_appearance.TryGetData<bool>(uid, AlertLevelDisplay.Powered, out var powered, component: args.Component))
        {
            _sprite.LayerSetVisible((uid, args.Sprite), layer, powered);
        }

        if (!_appearance.TryGetData<ProtoId<AlertLevelPrototype>>(uid, AlertLevelDisplay.CurrentLevel, out var level, component: args.Component))
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), layer, alertLevelDisplay.AlertVisuals.Values.First());
            return;
        }

        if (alertLevelDisplay.AlertVisuals.TryGetValue(level, out var visual))
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), layer, visual);
        }
        else
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), layer, alertLevelDisplay.AlertVisuals.Values.First());
        }
    }
}
