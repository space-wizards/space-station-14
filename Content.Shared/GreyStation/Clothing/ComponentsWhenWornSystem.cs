using Content.Shared.Clothing;

namespace Content.Shared.GreyStation.Clothing;

public sealed class ComponentsWhenWornSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComponentsWhenWornComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ComponentsWhenWornComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<ComponentsWhenWornComponent> ent, ref ClothingGotEquippedEvent args)
    {
        EntityManager.AddComponents(args.Wearer, ent.Comp.Components);
    }

    private void OnUnequipped(Entity<ComponentsWhenWornComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        EntityManager.RemoveComponents(args.Wearer, ent.Comp.Components);
    }
}
