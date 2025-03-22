using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Robust.Shared.Utility;

namespace Content.Shared.Armor;

/// <summary>
///     This handles logic relating to <see cref="ComplexArmorComponent" />
/// </summary>
public abstract class ComplexArmorSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ComplexArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid, ComplexArmorComponent comp, InventoryRelayedEvent<DamageModifyEvent> args)
    {
        var clothingUidList = _inventorySystem.GetHandOrInventoryEntities(uid, comp.Slots);

        foreach (var clothingUid in clothingUidList)
        {
            if (_tag.HasTag(clothingUid, comp.ClothingTag))
                args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, comp.Modifiers);
        }
    }
}
