using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private void InitializeVisuals()
    {
        SubscribeLocalEvent<StationAiCoreComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, StationAiCoreComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        if (sprite == null)
            return;
        if (!_appearance.TryGetData<string>(uid, StationAiCustomVisualState.Key, out var proto))
            return;
        if (!_proto.TryIndex<StationAIScreenPrototype>(proto, out var screen))
            return;

        sprite.LayerSetSprite(StationAiVisualState.Key, screen.Icon);
    }
}
