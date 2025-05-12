// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.Factory.Filters;

public sealed class AutomationFilterSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    private EntityQuery<FilterSlotComponent> _slotQuery;
    private EntityQuery<LabelComponent> _labelQuery;
    private EntityQuery<StackComponent> _stackQuery;

    public static readonly int GateCount = Enum.GetValues(typeof(LogicGate)).Length;

    public override void Initialize()
    {
        base.Initialize();

        _slotQuery = GetEntityQuery<FilterSlotComponent>();
        _labelQuery = GetEntityQuery<LabelComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();

        Subs.BuiEvents<LabelFilterComponent>(LabelFilterUiKey.Key, subs =>
        {
            subs.Event<LabelFilterSetLabelMessage>(OnLabelSet);
        });
        SubscribeLocalEvent<LabelFilterComponent, ExaminedEvent>(OnLabelExamined);
        SubscribeLocalEvent<LabelFilterComponent, AutomationFilterEvent>(OnLabelFilter);

        Subs.BuiEvents<NameFilterComponent>(NameFilterUiKey.Key, subs =>
        {
            subs.Event<NameFilterSetNameMessage>(OnNameSet);
            subs.Event<NameFilterSetModeMessage>(OnNameSetMode);
        });
        SubscribeLocalEvent<NameFilterComponent, ExaminedEvent>(OnNameExamined);
        SubscribeLocalEvent<NameFilterComponent, AutomationFilterEvent>(OnNameFilter);

        Subs.BuiEvents<StackFilterComponent>(StackFilterUiKey.Key, subs =>
        {
            subs.Event<StackFilterSetMinMessage>(OnStackSetMin);
            subs.Event<StackFilterSetSizeMessage>(OnStackSetSize);
        });
        SubscribeLocalEvent<StackFilterComponent, ExaminedEvent>(OnStackExamined);
        SubscribeLocalEvent<StackFilterComponent, AutomationFilterEvent>(OnStackFilter);
        SubscribeLocalEvent<StackFilterComponent, AutomationFilterSplitEvent>(OnStackSplit);

        SubscribeLocalEvent<CombinedFilterComponent, ComponentInit>(OnCombinedInit);
        SubscribeLocalEvent<CombinedFilterComponent, UseInHandEvent>(OnCombinedUse);
        SubscribeLocalEvent<CombinedFilterComponent, ExaminedEvent>(OnCombinedExamined);
        SubscribeLocalEvent<CombinedFilterComponent, AutomationFilterEvent>(OnCombinedFilter);
        SubscribeLocalEvent<CombinedFilterComponent, AutomationFilterSplitEvent>(OnCombinedSplit);

        SubscribeLocalEvent<FilterSlotComponent, ComponentInit>(OnSlotInit);
    }

    /* Label filter */

    private void OnLabelSet(Entity<LabelFilterComponent> ent, ref LabelFilterSetLabelMessage args)
    {
        var label = args.Label.Trim();
        if (label.Length > ent.Comp.MaxLength)
            return;

        ent.Comp.Label = label;
        Dirty(ent);
    }

    private void OnLabelExamined(Entity<LabelFilterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (string.IsNullOrEmpty(ent.Comp.Label))
        {
            args.PushMarkup(Loc.GetString("automation-filter-examine-empty"));
            return;
        }

        args.PushText(Loc.GetString("automation-filter-examine-string", ("name", ent.Comp.Label)));
    }

    private void OnLabelFilter(Entity<LabelFilterComponent> ent, ref AutomationFilterEvent args)
    {
        args.Allowed = _labelQuery.CompOrNull(args.Item)?.CurrentLabel == ent.Comp.Label;
    }

    /* Name filter */

    private void OnNameSet(Entity<NameFilterComponent> ent, ref NameFilterSetNameMessage args)
    {
        var name = args.Name.Trim();
        if (name.Length > ent.Comp.MaxLength || ent.Comp.Name == name)
            return;

        ent.Comp.Name = name;
        Dirty(ent);
    }

    private void OnNameSetMode(Entity<NameFilterComponent> ent, ref NameFilterSetModeMessage args)
    {
        if (ent.Comp.Mode == args.Mode)
            return;

        ent.Comp.Mode = args.Mode;
        Dirty(ent);
    }

    private void OnNameExamined(Entity<NameFilterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (string.IsNullOrEmpty(ent.Comp.Name))
        {
            args.PushMarkup(Loc.GetString("automation-filter-examine-empty"));
            return;
        }

        args.PushText(Loc.GetString("automation-filter-examine-string", ("name", ent.Comp.Name)));
    }

    private void OnNameFilter(Entity<NameFilterComponent> ent, ref AutomationFilterEvent args)
    {
        var name = Name(args.Item);
        var check = ent.Comp.Name;
        args.Allowed = ent.Comp.Mode switch
        {
            NameFilterMode.Contain => name.Contains(check),
            NameFilterMode.Start => name.StartsWith(check),
            NameFilterMode.End => name.EndsWith(check),
            NameFilterMode.Match => name == check
        };
    }

    private void OnCombinedInit(Entity<CombinedFilterComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;

        if (!_slots.TryGetSlot(ent, CombinedFilterComponent.FilterAName, out var filterA, slots) ||
            !_slots.TryGetSlot(ent, CombinedFilterComponent.FilterBName, out var filterB, slots))
        {
            Log.Error($"{ToPrettyString(ent)} was missing filter slots!");
            RemCompDeferred<CombinedFilterComponent>(ent);
            return;
        }

        ent.Comp.FilterA = filterA;
        ent.Comp.FilterB = filterB;
    }

    /* Stack filter */

    private void OnStackSetMin(Entity<StackFilterComponent> ent, ref StackFilterSetMinMessage args)
    {
        if (args.Min < 1 || ent.Comp.Min == args.Min)
            return;

        ent.Comp.Min = args.Min;
        Dirty(ent);
    }

    private void OnStackSetSize(Entity<StackFilterComponent> ent, ref StackFilterSetSizeMessage args)
    {
        if (args.Size < 0 || ent.Comp.Size == args.Size)
            return;

        ent.Comp.Size = args.Size;
        Dirty(ent);
    }

    private void OnStackExamined(Entity<StackFilterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("stack-filter-examine", ("size", ent.Comp.Size)));
    }

    private void OnStackFilter(Entity<StackFilterComponent> ent, ref AutomationFilterEvent args)
    {
        args.Allowed = _stackQuery.CompOrNull(args.Item)?.Count >= ent.Comp.Min;
    }

    private void OnStackSplit(Entity<StackFilterComponent> ent, ref AutomationFilterSplitEvent args)
    {
        args.Size = ent.Comp.Size;
    }

    /* Combined filter */

    private void OnCombinedUse(Entity<CombinedFilterComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var gate = (int) ent.Comp.Gate;
        gate = ++gate % GateCount;
        ent.Comp.Gate = (LogicGate) gate;
        Dirty(ent);

        var msg = Loc.GetString("logic-gate-cycle", ("gate", ent.Comp.Gate.ToString().ToUpper()));
        _popup.PopupClient(msg, ent, args.User);
    }

    private void OnCombinedExamined(Entity<CombinedFilterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("combined-filter-examine", ("gate", ent.Comp.Gate.ToString().ToUpper())));
    }

    private void OnCombinedFilter(Entity<CombinedFilterComponent> ent, ref AutomationFilterEvent args)
    {
        var a = IsAllowed(ent.Comp.FilterA.Item, args.Item);
        var b = IsAllowed(ent.Comp.FilterB.Item, args.Item);
        args.Allowed = ent.Comp.Gate switch
        {
            LogicGate.Or => a || b,
            LogicGate.And => a && b,
            LogicGate.Xor => a != b,
            LogicGate.Nor => !(a || b),
            LogicGate.Nand => !(a && b),
            LogicGate.Xnor => a == b
        };
    }

    private void OnCombinedSplit(Entity<CombinedFilterComponent> ent, ref AutomationFilterSplitEvent args)
    {
        var a = GetSplitSize(ent.Comp.FilterA.Item);
        var b = GetSplitSize(ent.Comp.FilterB.Item);
        args.Size = Math.Max(a, b);
    }

    private void OnSlotInit(Entity<FilterSlotComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;

        if (!_slots.TryGetSlot(ent, ent.Comp.FilterSlotId, out var filterSlot, slots))
        {
            Log.Warning($"Missing filter slot {ent.Comp.FilterSlotId} on {ToPrettyString(ent)}");
            RemCompDeferred<FilterSlotComponent>(ent);
            return;
        }

        ent.Comp.FilterSlot = filterSlot;
    }

    #region Public API
    /// <summary>
    /// Returns true if an item is allowed by the filter, false if it's blocked.
    /// If there is no filter, items are always allowed.
    /// </summary>
    public bool IsAllowed(EntityUid? filter, EntityUid item)
    {
        if (filter is not {} uid)
            return true;

        var ev = new AutomationFilterEvent(item);
        RaiseLocalEvent(uid, ref ev);
        return ev.Allowed;
    }

    /// <summary>
    /// Inverse of <see cref="IsAllowed"/>.
    /// </summary>
    public bool IsBlocked(EntityUid? filter, EntityUid item) => !IsAllowed(filter, item);

    /// <summary>
    /// Gets the split size for a filter.
    /// If non-zero then the pulled item is split into a multiple of the return value.
    /// If zero then nothing special is done.
    /// </summary>
    public int GetSplitSize(EntityUid? filter)
    {
        if (filter is not {} uid)
            return 0;

        var ev = new AutomationFilterSplitEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Size;
    }

    public EntityUid? TrySplit(EntityUid? filter, EntityUid item)
    {
        // if it's 0 don't need to split, take the item out directly
        var split = GetSplitSize(filter);
        if (split == 0)
            return item;

        // don't need to split if it's already a multiple of the split size
        var stack = Comp<StackComponent>(item);
        var excess = stack.Count % split;
        if (excess == 0)
            return item;

        // have to split it, client will return null here
        var coords = Transform(item).Coordinates;
        return _stack.Split(item, stack.Count - excess, coords, stack);
    }

    /// <summary>
    /// Get the filter in a machine's filter slot, or null if it has none.
    /// </summary>
    public EntityUid? GetSlot(EntityUid uid)
    {
        return _slotQuery.CompOrNull(uid)?.Filter;
    }
    #endregion
}
