using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.NameModifier.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.NameModifier.EntitySystems;

/// <inheritdoc cref="NameModifierComponent"/>
public sealed partial class NameModifierSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;

    [Dependency] private EntityQuery<NameModifierComponent> _nameModifierQuery;

    [SubscribeLocalEvent]
    private void OnEntityRenamed(Entity<NameModifierComponent> ent, ref EntityRenamedEvent args)
    {
        SetBaseName(ent, args.NewName);
        RefreshNameModifiers((ent.Owner, ent.Comp));
    }

    [SubscribeLocalEvent]
    private void OnGotEquipped(Entity<ModifyWearerNameComponent> _, ref ClothingGotEquippedEvent args)
    {
        RefreshNameModifiers(args.Wearer);
    }

    [SubscribeLocalEvent]
    private void OnGotUnequipped(Entity<ModifyWearerNameComponent> _, ref ClothingGotUnequippedEvent args)
    {
        RefreshNameModifiers(args.Wearer);
    }

    [SubscribeLocalEvent]
    private void OnRefreshNameModifiers(Entity<ModifyWearerNameComponent> entity, ref InventoryRelayedEvent<RefreshNameModifiersEvent> args)
    {
        args.Args.AddModifier(entity.Comp.LocId, entity.Comp.Priority);
    }

    [SubscribeLocalEvent]
    private void OnStatusApplied(Entity<ModifyNameStatusEffectComponent> _, ref StatusEffectAppliedEvent args)
    {
        RefreshNameModifiers(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnStatusRemoved(Entity<ModifyNameStatusEffectComponent> _, ref StatusEffectRemovedEvent args)
    {
        RefreshNameModifiers(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnStatusRefreshNameModifiers(Entity<ModifyNameStatusEffectComponent> entity, ref StatusEffectRelayedEvent<RefreshNameModifiersEvent> args)
    {
        args.Args.AddModifier(entity.Comp.LocId, entity.Comp.Priority);
    }
}
