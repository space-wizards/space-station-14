using Content.Client.Items.Systems;
using Content.Shared.Clothing.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.Systems;

public sealed class ChameleonClothingVisualizerSystem : VisualizerSystem<ChameleonClothingComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ItemSystem _itemSystem = default!;

    protected override void OnAppearanceChange(EntityUid uid, ChameleonClothingComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!args.Component.TryGetData(ChameleonVisuals.ClothingId, out string? protoId))
            return;
        if (!_proto.TryIndex(protoId, out EntityPrototype? proto))
            return;

        // world sprite icon
        if (TryComp(uid, out SpriteComponent? sprite)
            && proto.TryGetComponent(out SpriteComponent? otherSprite))
        {
            sprite.CopyFrom(otherSprite);
        }

        if (TryComp(uid, out ClothingComponent? clothing) &&
            proto.TryGetComponent(out ClothingComponent? otherClothing))
        {
            _itemSystem.CopyVisuals(uid, otherClothing, clothing);
        }
    }
}
