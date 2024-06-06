using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.NameModifier.Components;

namespace Content.Shared.NameModifier.EntitySystems;

public sealed partial class ModifyWearerNameSystem : EntitySystem
{
    [Dependency] private readonly NameModifierSystem _renamer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyWearerNameComponent, InventoryRelayedEvent<RefreshNameModifiersEvent>>(OnRefreshNameModifiers);
        SubscribeLocalEvent<ModifyWearerNameComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ModifyWearerNameComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<ModifyWearerNameComponent> entity, ref ClothingGotEquippedEvent args)
    {
        _renamer.RefreshNameModifiers(args.Wearer);
    }

    private void OnGotUnequipped(Entity<ModifyWearerNameComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        _renamer.RefreshNameModifiers(args.Wearer);
    }

    private void OnRefreshNameModifiers(Entity<ModifyWearerNameComponent> entity, ref InventoryRelayedEvent<RefreshNameModifiersEvent> args)
    {
        switch (entity.Comp.ModifierType)
        {
            case NameModifierType.Prefix:
                {
                    args.Args.AddPrefix(Loc.GetString(entity.Comp.Text), entity.Comp.Priority);
                    break;
                }
            case NameModifierType.Postfix:
                {
                    args.Args.AddPostfix(Loc.GetString(entity.Comp.Text), entity.Comp.Priority);
                    break;
                }
            case NameModifierType.Override:
                {
                    args.Args.AddOverride(Loc.GetString(entity.Comp.Text), entity.Comp.Priority);
                    break;
                }
        }
    }
}
