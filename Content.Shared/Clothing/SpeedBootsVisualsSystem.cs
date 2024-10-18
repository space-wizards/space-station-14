using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Clothing;

public sealed partial class SpeedBootsVisualsSystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeedBootsVisualsComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnToggled(Entity<SpeedBootsVisualsComponent> entity, ref ItemToggledEvent args)
    {
        var prefix = args.Activated ? "on" : null;
        _item.SetHeldPrefix(entity, prefix);
        _clothing.SetEquippedPrefix(entity, prefix);
    }
}
