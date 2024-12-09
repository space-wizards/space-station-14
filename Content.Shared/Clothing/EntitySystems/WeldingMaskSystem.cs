using Content.Shared.Clothing.Components;
using Content.Shared.Eye;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class WeldingMaskSystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedDarkenedVisionSystem _darkenedVision = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeldingMaskComponent, ItemToggledEvent>(OnToggle);
    }

    private void OnToggle(Entity<WeldingMaskComponent> ent, ref ItemToggledEvent args)
    {
        var prefix = args.Activated ? null : "up";
        _clothing.SetEquippedPrefix(ent, prefix);
        _item.SetHeldPrefix(ent, prefix);

        if (args.User is not { } user) return;

        _darkenedVision.UpdateVisionDarkening(user);

        // update identity
        var ev = new WearerMaskToggledEvent(IsToggled: args.Activated);
        RaiseLocalEvent(user, ref ev);
    }
}
