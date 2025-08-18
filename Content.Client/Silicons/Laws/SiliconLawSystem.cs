using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawOverriderComponent, AppearanceChangeEvent>(OnLawOverriderAppearanceChanged);
    }

    public void OnLawOverriderAppearanceChanged(EntityUid uid, SiliconLawOverriderComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        var sprite = args.Sprite;

        if (!_appearance.TryGetData(uid, LawOverriderVisuals.LawBoardInserted, out bool lawboardInserted))
            lawboardInserted = false;

        _sprite.LayerSetVisible((uid, sprite), LawOverriderVisualLayers.LawBoard, lawboardInserted);
        _sprite.LayerSetVisible((uid, sprite), LawOverriderVisualLayers.Light, lawboardInserted);
    }
}
