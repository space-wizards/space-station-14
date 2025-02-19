using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class HeadphonesSystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadphonesComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnToggled(Entity<HeadphonesComponent> ent, ref ItemToggledEvent args)
    {
        var (uid, comp) = ent;
        var prefix = args.Activated ? "on" : null;
        _item.SetHeldPrefix(ent, prefix);
        _clothing.SetEquippedPrefix(ent, prefix);
    }

}
