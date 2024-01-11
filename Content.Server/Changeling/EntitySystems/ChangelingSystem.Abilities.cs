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

        if (!component.ArmBladeActive)
        {
            if (!TryUseAbility(uid, component, component.ArmBladeChemicalsCost))
                return;

            var armblade = Spawn(ArmBladeId, Transform(uid).Coordinates);
            EnsureComp<UnremoveableComponent>(armblade); // armblade is apart of your body.. cant remove it..

            if (_handsSystem.TryGetEmptyHand(uid, out var emptyHand, handsComponent))
            {
                _handsSystem.TryPickup(uid, armblade, emptyHand, false, false, handsComponent);
                component.ArmBladeActive = true;
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
                        component.ArmBladeActive = false;
                    }
                }
            }
        }
    }

    public const string LingHelmetId = "ClothingHeadHelmetLing";
    public const string LingArmorId = "ClothingOuterArmorChangeling";

    private void OnLingArmorAction(EntityUid uid, ChangelingComponent component, LingArmorActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp(uid, out InventoryComponent? inventory))
            return;

        if (!component.LingArmorActive)
        {
            if (!TryUseAbility(uid, component, component.LingArmorChemicalsCost))
                return;

            var helmet = Spawn(LingHelmetId, Transform(uid).Coordinates);
            var armor = Spawn(LingArmorId, Transform(uid).Coordinates);
            EnsureComp<UnremoveableComponent>(helmet); // cant remove the armor
            EnsureComp<UnremoveableComponent>(armor); // cant remove the armor

            _inventorySystem.TryUnequip(uid, "head", true, true, false, inventory);
            _inventorySystem.TryEquip(uid, helmet, "head", true, true, false, inventory);
            _inventorySystem.TryUnequip(uid, "outerclothing", true, true, false, inventory);
            _inventorySystem.TryEquip(uid, armor, "outerclothing", true, true, false, inventory);
            component.LingArmorActive = true;
        }
        else
        {
            if (_inventorySystem.TryGetSlotEntity(uid, "head", out var headitem) && _inventorySystem.TryGetSlotEntity(uid, "outerclothing", out var outerclothingitem))
            {
                if (TryPrototype(headitem.Value, out var headitemproto))
                {
                    var result = _proto.HasIndex<EntityPrototype>(LingHelmetId);
                    if (result)
                    {
                        QueueDel(headitem.Value);
                    }
                }

                if (TryPrototype(outerclothingitem.Value, out var outerclothingproto))
                {
                    var result = _proto.HasIndex<EntityPrototype>(LingArmorId);
                    if (result)
                    {
                        QueueDel(outerclothingitem.Value);
                    }
                }
            }

            component.LingArmorActive = false;
        }
    }
}
