using Content.Client.Items.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.Systems;

public sealed class ChameleonClothingVisualizerSystem : VisualizerSystem<ChameleonClothingComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ItemSystem _itemSystem = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;

    protected override void OnAppearanceChange(EntityUid uid, ChameleonClothingComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!args.Component.TryGetData(ChameleonVisuals.ClothingId, out string? protoId))
            return;
        if (!_proto.TryIndex(protoId, out EntityPrototype? proto))
            return;

        // world sprite icon
        if (TryComp(uid, out SpriteComponent? sprite)
            && proto.TryGetComponent(out SpriteComponent? otherSprite, _factory))
        {
            sprite.CopyFrom(otherSprite);
        }

        // clothing and in-hand sprite icon
        if (TryComp(uid, out ItemComponent? clothing) &&
            proto.TryGetComponent(out ItemComponent? otherClothing, _factory))
        {
            _itemSystem.CopyVisuals(uid, otherClothing, clothing);
        }
    }
}
