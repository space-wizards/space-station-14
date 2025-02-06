using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Armor;

/// <summary>
///     This handles logic relating to <see cref="ArmorWithHelmetCompoent" />
/// </summary>
public abstract class ArmorWithHelmetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorWithClothingComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid, ArmorWithHelmetComponent component, InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if !_inventorySystem.TryGetInventoryEntity<ArmorHelmetComponent>(uid, out var protectiveEntity);
            return;

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, component.Modifiers);
    }
}
