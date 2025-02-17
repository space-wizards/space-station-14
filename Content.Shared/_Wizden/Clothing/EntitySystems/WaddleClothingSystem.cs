using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class WaddleClothingSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<WaddleWhenWornComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnGotEquipped(Entity<WaddleWhenWornComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if(_toggle.IsActivated(ent.Owner))
            AddAnimationComponent(ent, args.Wearer);
    }

    private void OnGotUnequipped(Entity<WaddleWhenWornComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemoveAnimationComponent(ent, args.Wearer);
    }

    private void OnToggled(Entity<WaddleWhenWornComponent> ent, ref ItemToggledEvent args)
    {
        var (uid, comp) = ent;
        // ensure clothing is in correct slot
        if (_container.TryGetContainingContainer((uid, null, null), out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, comp.Slot, out var worn)
            && uid == worn)
        {
            {
                if (args.Activated)
                    AddAnimationComponent(ent, container.Owner);
                else
                    RemoveAnimationComponent(ent, container.Owner);
            }
        }
    }

    private void AddAnimationComponent(Entity<WaddleWhenWornComponent> ent, EntityUid wearer)
    {
        var waddleAnimComp = EnsureComp<WaddleAnimationComponent>(wearer);

        waddleAnimComp.AnimationLength = ent.Comp.AnimationLength;
        waddleAnimComp.HopIntensity = ent.Comp.HopIntensity;
        waddleAnimComp.RunAnimationLengthMultiplier = ent.Comp.RunAnimationLengthMultiplier;
        waddleAnimComp.TumbleIntensity = ent.Comp.TumbleIntensity;
        _alerts.ShowAlert(wearer, ent.Comp.WaddlingAlert);
    }

    private void RemoveAnimationComponent(Entity<WaddleWhenWornComponent> ent, EntityUid wearer)
    {
        RemComp<WaddleAnimationComponent>(wearer);
        _alerts.ClearAlert(wearer, ent.Comp.WaddlingAlert);
    }
}
