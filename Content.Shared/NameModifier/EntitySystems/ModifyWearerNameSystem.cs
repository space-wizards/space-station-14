using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.NameModifier.Components;

namespace Content.Shared.NameModifier.EntitySystems;

public sealed partial class ModifyWearerNameSystem : EntitySystem
{
    [Dependency] private readonly NameModifierSystem _nameMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyWearerNameComponent, InventoryRelayedEvent<RefreshNameModifiersEvent>>(OnRefreshNameModifiers);
        SubscribeLocalEvent<ModifyWearerNameComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ModifyWearerNameComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<ModifyWearerNameComponent> entity, ref ClothingGotEquippedEvent args)
    {
        _nameMod.RefreshNameModifiers(args.Wearer);
    }

    private void OnGotUnequipped(Entity<ModifyWearerNameComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        _nameMod.RefreshNameModifiers(args.Wearer);
    }

    private void OnRefreshNameModifiers(Entity<ModifyWearerNameComponent> entity, ref InventoryRelayedEvent<RefreshNameModifiersEvent> args)
    {
        args.Args.AddModifier(entity.Comp.LocId, entity.Comp.Priority);
    }
}
