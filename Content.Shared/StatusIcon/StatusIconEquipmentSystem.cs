using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusIcon.Components;

namespace Content.Shared.StatusIcon;

public sealed class StatusIconEquipmentSystem : EntitySystem
{
    [Dependency] private readonly SharedStatusIconSystem _statusIconSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconEquipmentComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<StatusIconEquipmentComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<StatusIconEquipmentComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<StatusIconEquipmentComponent, GotUnequippedHandEvent>(OnUnequippedHand);
    }

    private void OnEquipped(EntityUid uid, StatusIconEquipmentComponent comp, ref GotEquippedEvent args)
    {
        if ((comp.Slots & args.SlotFlags) == 0)
            return;

        _statusIconSystem.AddTemporaryStatusIcon(args.Equipee);
    }

    private void OnEquippedHand(EntityUid uid, StatusIconEquipmentComponent comp, ref GotEquippedHandEvent args)
    {
        if (!comp.IncludeHands)
            return;

        _statusIconSystem.AddTemporaryStatusIcon(args.User);
    }

    private void OnUnequipped(EntityUid uid, StatusIconEquipmentComponent comp, ref GotUnequippedEvent args)
    {
        if ((comp.Slots & args.SlotFlags) == 0)
            return;

        _statusIconSystem.RemoveTemporaryStatusIcon(args.Equipee);
    }

    private void OnUnequippedHand(EntityUid uid, StatusIconEquipmentComponent comp, ref GotUnequippedHandEvent args)
    {
        if (!comp.IncludeHands)
            return;

        _statusIconSystem.RemoveTemporaryStatusIcon(args.User);
    }
}
