using Content.Client.Items.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.Systems;

public sealed class ChameleonClothingVisualizerSystem : VisualizerSystem<ChameleonClothingComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ItemSystem _itemSystem = default!;
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
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

        // item sprite logic
        if (TryComp(uid, out ItemComponent? item) &&
            proto.TryGetComponent(out ItemComponent? otherItem, _factory))
        {
            _itemSystem.CopyVisuals(uid, otherItem, item);
        }

        // clothing sprite logic
        if (TryComp(uid, out ClothingComponent? clothing) &&
            proto.TryGetComponent(out ClothingComponent? otherClothing, _factory))
        {
            _clothingSystem.CopyVisuals(uid, otherClothing, clothing);
        }
    }
}
