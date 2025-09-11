using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
///     A system for the operation of a component that prohibits the player from taking off his own clothes that have this component.
/// </summary>
public sealed class SelfUnremovableClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfUnremovableClothingComponent, BeingUnequippedAttemptEvent>(OnUnequip);
        SubscribeLocalEvent<SelfUnremovableClothingComponent, ExaminedEvent>(OnUnequipMarkup);
    }

    private void OnUnequip(Entity<SelfUnremovableClothingComponent> selfUnremovableClothing, ref BeingUnequippedAttemptEvent args)
    {
        if (TryComp<ClothingComponent>(selfUnremovableClothing, out var clothing) && (clothing.Slots & args.SlotFlags) == SlotFlags.NONE)
            return;

        if (args.UnEquipTarget == args.Unequipee)
        {
            args.Cancel();
        }
    }

    private void OnUnequipMarkup(Entity<SelfUnremovableClothingComponent> selfUnremovableClothing, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("comp-self-unremovable-clothing"));
    }
}
