using Content.Client.PDA;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.Systems;

// All valid items for chameleon are calculated on client startup and stored in dictionary.
public sealed class ChameleonClothingSystem : SharedChameleonClothingSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, AfterAutoHandleStateEvent>(HandleState);
    }

    private void HandleState(EntityUid uid, ChameleonClothingComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(uid, component);
    }

    protected override void UpdateSprite(EntityUid uid, EntityPrototype proto)
    {
        base.UpdateSprite(uid, proto);
        if (TryComp(uid, out SpriteComponent? sprite)
            && proto.TryGetComponent(out SpriteComponent? otherSprite, Factory))
        {
            sprite.CopyFrom(otherSprite);
        }
    }

    protected override void UpdateVisuals(EntityUid uid, ChameleonClothingComponent component, EntityPrototype? prototype = null)
    {
        if (!component.Default.HasValue)
            return;

        if (prototype == null)
            prototype = Proto.Index(component.Default.Value);

        base.UpdateVisuals(uid, component, prototype);

        if (TryComp<PdaBorderColorComponent>(uid, out var pdaBorder) &&
            prototype.TryGetComponent<PdaBorderColorComponent>(out var otherPdaBorder))
        {
            pdaBorder.AccentHColor = otherPdaBorder.AccentHColor;
            pdaBorder.AccentVColor = otherPdaBorder.AccentVColor;
            pdaBorder.BorderColor = otherPdaBorder.BorderColor;
        }
    }
}
