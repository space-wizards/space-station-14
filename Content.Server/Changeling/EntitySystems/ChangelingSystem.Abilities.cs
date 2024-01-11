using Content.Shared.Changeling.Components;
using Content.Shared.Changeling;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands.Components;
using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    private void InitializeLingAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, ArmBladeActionEvent>(OnArmBladeAction);
        SubscribeLocalEvent<ChangelingComponent, LingArmorActionEvent>(OnLingArmorAction);
    }

    public const string ArmBladeId = "ArmBlade";
    private void OnArmBladeAction(EntityUid uid, ChangelingComponent component, ArmBladeActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp(uid, out HandsComponent? handsComponent))
            return;
        if (handsComponent.ActiveHand == null)
            return;

        var handContainer = handsComponent.ActiveHand.Container;

        if (handContainer == null)
            return;

        if (!TryUseAbility(uid, component, component.ArmBladeChemicalsCost, !component.ArmBladeActive))
            return;

        if (!component.ArmBladeActive)
        {
            var armblade = Spawn(ArmBladeId, Transform(uid).Coordinates);
            EnsureComp<UnremoveableComponent>(armblade); // armblade is apart of your body.. cant remove it..

            if (_handsSystem.TryGetEmptyHand(uid, out var emptyHand, handsComponent))
            {
                _handsSystem.TryPickup(uid, armblade, emptyHand, false, false, handsComponent);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("changeling-armblade-fail"), uid, uid);
                QueueDel(armblade);
            }
        }
        else
        {
            if (handContainer.ContainedEntity != null)
            {
                if (TryPrototype(handContainer.ContainedEntity.Value, out var protoInHand))
                {
                    var result = _proto.HasIndex<EntityPrototype>(ArmBladeId);
                    if (result)
                    {
                        QueueDel(handContainer.ContainedEntity.Value);
                    }
                }
            }
        }

        component.ArmBladeActive = !component.ArmBladeActive;
    }

    public const string LingHelmetId = "ClothingHeadHelmetLing";
    public const string LingArmorId = "ClothingOuterArmorChangeling";
    public const string HeadId = "head";
    public const string OuterClothingId = "outerClothing";

    private void OnLingArmorAction(EntityUid uid, ChangelingComponent component, LingArmorActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp(uid, out InventoryComponent? inventory))
            return;

        if (!TryUseAbility(uid, component, component.LingArmorChemicalsCost, !component.LingArmorActive, component.LingArmorRegenCost))
            return;

        if (!component.LingArmorActive)
        {
            var helmet = Spawn(LingHelmetId, Transform(uid).Coordinates);
            var armor = Spawn(LingArmorId, Transform(uid).Coordinates);
            EnsureComp<UnremoveableComponent>(helmet); // cant remove the armor
            EnsureComp<UnremoveableComponent>(armor); // cant remove the armor

            _inventorySystem.TryUnequip(uid, HeadId, true, true, false, inventory);
            _inventorySystem.TryEquip(uid, helmet, HeadId, true, true, false, inventory);
            _inventorySystem.TryUnequip(uid, OuterClothingId, true, true, false, inventory);
            _inventorySystem.TryEquip(uid, armor, OuterClothingId, true, true, false, inventory);
        }
        else
        {
            if (_inventorySystem.TryGetSlotEntity(uid, HeadId, out var headitem) && _inventorySystem.TryGetSlotEntity(uid, OuterClothingId, out var outerclothingitem))
            {
                if (TryPrototype(headitem.Value, out var headitemproto))
                {
                    var result = _proto.HasIndex<EntityPrototype>(LingHelmetId);
                    if (result)
                    {
                        _inventorySystem.TryUnequip(uid, HeadId, true, true, false, inventory);
                        QueueDel(headitem.Value);
                    }
                }

                if (TryPrototype(outerclothingitem.Value, out var outerclothingproto))
                {
                    var result = _proto.HasIndex<EntityPrototype>(LingArmorId);
                    if (result)
                    {
                        _inventorySystem.TryUnequip(uid, OuterClothingId, true, true, false, inventory);
                        QueueDel(outerclothingitem.Value);
                    }
                }
            }
        }

        component.LingArmorActive = !component.LingArmorActive;
    }
}
