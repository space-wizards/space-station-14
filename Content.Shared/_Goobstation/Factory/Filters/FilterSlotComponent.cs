// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Factory.Filters;

/// <summary>
/// Component for machines that have a filter slot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AutomationFilterSystem))]
public sealed partial class FilterSlotComponent : Component
{
    /// <summary>
    /// Item slot that stores a filter.
    /// </summary>
    [DataField]
    public string FilterSlotId = "filter_slot";

    /// <summary>
    /// The filter slot cached on init.
    /// </summary>
    [ViewVariables]
    public ItemSlot FilterSlot = default!;

    /// <summary>
    /// The currently inserted filter.
    /// </summary>
    [ViewVariables]
    public EntityUid? Filter => FilterSlot.Item;
}
