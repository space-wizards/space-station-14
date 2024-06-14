using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Shared.PAI;

/// <summary>
/// Handles pAI expansion cards and slots.
/// </summary>
public sealed class PAIExpansionSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedWiresSystem _wires = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAIExpansionCardComponent, ExaminedEvent>(OnCardExamined);

        SubscribeLocalEvent<PAIExpansionSlotComponent, ExaminedEvent>(OnSlotExamined);
        SubscribeLocalEvent<PAIExpansionSlotComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<PAIExpansionSlotComponent, EntInsertedIntoContainerMessage>(OnCardInserted);
    }

    private void OnCardExamined(Entity<PAIExpansionCardComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("pai-expansion-card-examined"));
    }

    private void OnSlotExamined(Entity<PAIExpansionSlotComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !_wires.IsPanelOpen(ent.Owner))
            return;

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SlotId, out var slot))
            return;

        var msg = slot.Item is {} card
            ? Loc.GetString("pai-expansion-slot-filled", ("card", Name(card)))
            : Loc.GetString("pai-expansion-slot-empty");
        args.PushMarkup(msg);
    }

    private void OnItemSlotInsertAttempt(Entity<PAIExpansionSlotComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Slot.ID != ent.Comp.SlotId)
            return;

        // the itemslot has it whitelisted
        if (!TryComp<PAIExpansionCardComponent>(args.Item, out var card))
            return;

        if (!_wires.IsPanelOpen(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.PanelClosedPopup), ent, args.User);
            args.Cancelled = true;
            return;
        }

        if (_whitelist.IsWhitelistFail(card.Whitelist, ent))
        {
            _popup.PopupClient(Loc.GetString(card.WhitelistFailPopup), ent, args.User);
            args.Cancelled = true;
        }
    }

    private void OnCardInserted(Entity<PAIExpansionSlotComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SlotId)
            return;

        var card = Comp<PAIExpansionCardComponent>(args.Entity);
        _actions.AddAction(ent, ref card.ActionEntity, card.Action, ent);
        EntityManager.AddComponents(ent, card.Components);
    }
}
