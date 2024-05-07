using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Renamer.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Renamer.EntitySystems;

public sealed partial class ModifyWearerNameSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly RenamerSystem _renamer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyWearerNameComponent, InventoryRelayedEvent<RefreshNameModifiersEvent>>(OnRefreshNameModifiers);
        SubscribeLocalEvent<ModifyWearerNameComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ModifyWearerNameComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<ModifyWearerNameComponent> entity, ref ClothingGotEquippedEvent args)
    {
        TriggerNameModifiersRefresh(entity, args.Wearer);
    }

    private void OnGotUnequipped(Entity<ModifyWearerNameComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        TriggerNameModifiersRefresh(entity, args.Wearer);
    }
    private void OnRefreshNameModifiers(Entity<ModifyWearerNameComponent> entity, ref InventoryRelayedEvent<RefreshNameModifiersEvent> args)
    {
        switch (entity.Comp.ModifierType)
        {
            case NameModifierType.Prefix:
                {
                    args.Args.AddPrefix(entity.Comp.Text);
                    break;
                }
            case NameModifierType.Postfix:
                {
                    args.Args.AddPostfix(entity.Comp.Text);
                    break;
                }
            case NameModifierType.Override:
                {
                    args.Args.AddOverride(entity.Comp.Text);
                    break;
                }
        }
    }

    private void TriggerNameModifiersRefresh(Entity<ModifyWearerNameComponent> entity, EntityUid equipee)
    {
        _renamer.RefreshNameModifiers(equipee);
    }
}
