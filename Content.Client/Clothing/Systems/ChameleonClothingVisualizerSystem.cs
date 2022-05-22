using Content.Shared.Clothing.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.Systems;

public sealed class ChameleonClothingVisualizerSystem : VisualizerSystem<ChameleonClothingComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void OnAppearanceChange(EntityUid uid, ChameleonClothingComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;
        if (!args.Component.TryGetData(ChameleonVisuals.ClothingId, out string? protoId))
            return;
        if (!_proto.TryIndex(protoId, out EntityPrototype? proto))
            return;
        if (!proto.TryGetComponent<SpriteComponent>(out var otherSprite))
            return;
        sprite.CopyFrom(otherSprite);
    }
}
